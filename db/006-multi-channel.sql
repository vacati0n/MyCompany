-- Wave 6, the multi-channel capability. Additive throughout.
--
-- No delivered table, column, view or trigger is altered or dropped, every object here is created
-- only where it is absent, and every check it adds binds NEW ROWS ONLY, so every row already
-- recorded keeps the meaning it had. It is applied after every resource it depends on.
--
-- NOTHING HERE RECORDS A CHANNEL, a company, a budget, a configuration value, a payment-account
-- observation, a first-publication condition, an approval or a gate transition. The one row this
-- file writes is the chain-head row, which holds no value of any kind. The company-level
-- payment-account record is created EMPTY, so the payment-account condition of every channel reads
-- ABSENT and refuses until the owner records an observation, exactly as before this file existed.
--
-- Reversal is by dropping what this file creates, in reverse order. It is lossless while the
-- company-level record is empty; once an observation is recorded there, restore-to-point is the
-- reversal. Each of the checks on delivered records can be dropped on its own without loss.

BEGIN;

-- ---------------------------------------------------------------------------
-- The company-level payee: one payment-account observation record per company
-- ---------------------------------------------------------------------------

-- One legal entity, several channels: the payee and the payment account are COMPANY-LEVEL facts.
-- The observation shape is the delivered condition shape, three-valued with mandatory evidence, and
-- it is keyed by the company and the observation date, so no record shape admits a value per
-- channel and no two channels of one company can read the condition differently.
CREATE TABLE IF NOT EXISTS company_payment_account_observations (
    company_id  uuid NOT NULL REFERENCES companies (company_id),
    state       text NOT NULL CHECK (state IN ('Unknown', 'NotSatisfied', 'Satisfied')),

    -- An observation with no evidence is not one, exactly as on the per-channel table.
    evidence    text NOT NULL CHECK (length(btrim(evidence)) > 0),
    observed_on date NOT NULL,

    PRIMARY KEY (company_id, observed_on)
);

-- A NEW per-channel payment-account observation is refused. The check is NOT VALID, so it binds new
-- rows only: every observation already recorded is RETAINED, and it is no longer read to resolve the
-- condition, so nothing is lost and resolution can only become more refusing than before. How many
-- were retained is reported when the file is applied.
DO $$
DECLARE
    retained bigint;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'first_publication_conditions_no_new_channel_payment_account'
    ) THEN
        ALTER TABLE first_publication_conditions
            ADD CONSTRAINT first_publication_conditions_no_new_channel_payment_account
            CHECK (condition <> 'PaymentAccount') NOT VALID;
    END IF;

    SELECT count(*) INTO retained FROM first_publication_conditions WHERE condition = 'PaymentAccount';

    RAISE NOTICE 'per-channel payment-account observations retained and no longer read for resolution: %', retained;
END;
$$;

-- ---------------------------------------------------------------------------
-- The audit chain: one head, held by every writer of the append-only record
-- ---------------------------------------------------------------------------

-- ONE ROW, holding no value. Every appender takes it EXCLUSIVELY before it stamps its entry and
-- before it reads the head hash, and keeps it to the end of its transaction, so appenders serialise
-- and each reads a head no other in-flight appender can extend. It is the only waited-for exclusive
-- hold on the append-only record: every exclusive acquisition of the record horizon is non-waiting,
-- so a transaction holding the horizon shared and waiting here waits on a holder that needs only
-- shared holds, which no queued exclusive request can block, and no cycle can form.
CREATE TABLE IF NOT EXISTS audit_chain_head (
    only_row boolean PRIMARY KEY DEFAULT true CHECK (only_row)
);

INSERT INTO audit_chain_head (only_row) VALUES (true) ON CONFLICT (only_row) DO NOTHING;

-- THE CHECK, binding every writer of the append-only record, the appender and anything that
-- bypasses it alike. It takes the same hold, so a bypassing writer waits for an appender in flight
-- rather than racing it, and it refuses an entry whose predecessor hash is not the entry hash of
-- the highest-sequence entry, or the genesis value where the record holds none. The hold is taken
-- by touching the head row, so a writer working under a snapshot older than the latest append is
-- refused for a concurrent update rather than chaining after a head it cannot see. Historical pairs
-- that share a predecessor are neither repaired nor rejected: only the current head is read.
--
-- Its trigger is named to fire AFTER the delivered horizon check on the same event, so an entry
-- stamped below the horizon is still refused for that reason first.
CREATE OR REPLACE FUNCTION refuse_entry_off_chain_head() RETURNS trigger AS $$
DECLARE
    head text;
BEGIN
    UPDATE audit_chain_head SET only_row = only_row WHERE only_row;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'the audit chain head is missing; the sixth schema resource creates it'
            USING ERRCODE = 'integrity_constraint_violation';
    END IF;

    SELECT entry_hash INTO head FROM audit_entries ORDER BY sequence_no DESC LIMIT 1;

    IF NEW.previous_entry_hash IS DISTINCT FROM COALESCE(head, '') THEN
        RAISE EXCEPTION 'the audit chain is linear: entry % names predecessor % but the current head is %',
            NEW.entry_id, NEW.previous_entry_hash, COALESCE(head, '(genesis)')
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'audit_entries_chained_to_head';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER audit_entries_chained_to_head
    BEFORE INSERT ON audit_entries
    FOR EACH ROW EXECUTE FUNCTION refuse_entry_off_chain_head();

-- The chain-head row is never removed.
CREATE OR REPLACE FUNCTION refuse_chain_head_removal() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'the audit chain head is never removed: % refused', TG_OP
        USING ERRCODE = 'integrity_constraint_violation';
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER audit_chain_head_kept
    BEFORE DELETE ON audit_chain_head
    FOR EACH ROW EXECUTE FUNCTION refuse_chain_head_removal();

-- ---------------------------------------------------------------------------
-- The operation record: booked at the datastore's instant, under the record horizon
-- ---------------------------------------------------------------------------

-- THE CHECK, binding every writer of the operation record. It takes the record horizon SHARED,
-- held to the end of the writing transaction exactly as the appender's stamp is, and refuses an
-- operation stamped below the horizon or booked into any month other than its own instant's, in
-- UTC. A month the horizon has passed the end of can therefore never gain an operation, so a month
-- reading over the operation record presented as final is final by construction. An update that
-- would move an operation's instant or its month is refused outright, because it could take an
-- operation out of a closed month.
CREATE OR REPLACE FUNCTION refuse_operation_off_horizon() RETURNS trigger AS $$
DECLARE
    closed_below timestamptz;
BEGIN
    IF TG_OP = 'UPDATE' THEN
        IF NEW.occurred_at IS DISTINCT FROM OLD.occurred_at OR NEW.period IS DISTINCT FROM OLD.period THEN
            RAISE EXCEPTION 'an operation is booked once: moving operation % out of % is refused',
                OLD.operation_id, OLD.period
                USING ERRCODE = 'integrity_constraint_violation',
                      CONSTRAINT = 'agent_costs_booked_under_horizon';
        END IF;

        RETURN NEW;
    END IF;

    SELECT horizon INTO closed_below FROM audit_record_horizon WHERE only_row FOR SHARE;

    IF closed_below IS NOT NULL AND NEW.occurred_at < closed_below THEN
        RAISE EXCEPTION 'the operation record is closed below %: an operation stamped % is refused',
            closed_below, NEW.occurred_at
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'agent_costs_booked_under_horizon';
    END IF;

    IF NEW.period <> (date_trunc('month', NEW.occurred_at AT TIME ZONE 'UTC'))::date THEN
        RAISE EXCEPTION 'an operation is booked into the month of its instant: % stamped % is not in %',
            NEW.operation_id, NEW.occurred_at, NEW.period
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'agent_costs_booked_under_horizon';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER agent_costs_booked_under_horizon
    BEFORE INSERT OR UPDATE ON agent_costs
    FOR EACH ROW EXECUTE FUNCTION refuse_operation_off_horizon();

-- ---------------------------------------------------------------------------
-- The gate: every transition starts from the recorded state
-- ---------------------------------------------------------------------------

-- THE CHECK, binding every writer of the gate-transition record. It holds the item row against
-- other gate writers for the rest of the writing transaction, so two writers of one item
-- serialise, and it refuses a transition whose from-state is not the item version's latest
-- recorded state, Draft where none is recorded. No caller can claim a state the record does not
-- hold. The hold is the no-key exclusive form, so a foreign-key check on the item is not blocked.
--
-- Its trigger is named to fire AFTER the delivered bypass check on the same event, so a transition
-- into Published from anything but Approved is still refused for that reason first.
CREATE OR REPLACE FUNCTION refuse_transition_from_unrecorded_state() RETURNS trigger AS $$
DECLARE
    recorded text;
BEGIN
    PERFORM 1 FROM items WHERE item_id = NEW.item_id FOR NO KEY UPDATE;

    SELECT to_state INTO recorded
    FROM gate_transitions
    WHERE item_id = NEW.item_id AND item_version = NEW.item_version
    ORDER BY occurred_at DESC
    LIMIT 1;

    recorded := COALESCE(recorded, 'Draft');

    IF NEW.from_state <> recorded THEN
        RAISE EXCEPTION 'gate transition refused: item % version % is recorded in %, not in %',
            NEW.item_id, NEW.item_version, recorded, NEW.from_state
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'gate_transitions_from_recorded_state';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER gate_transitions_refuse_unrecorded_from_state
    BEFORE INSERT ON gate_transitions
    FOR EACH ROW EXECUTE FUNCTION refuse_transition_from_unrecorded_state();

COMMIT;
