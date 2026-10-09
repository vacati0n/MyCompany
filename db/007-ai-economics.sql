-- Wave 7, the AI-economics capability. Additive throughout.
--
-- Every object here is created only where it is absent, and every new check binds every writer of
-- the record it guards, the adapters and anything that bypasses them alike. It is applied after every
-- resource it depends on.
--
-- NOTHING HERE RECORDS AN OBSERVATION, a corpus entry, an admission decision, a budget amount, a
-- configuration value, a route or a gate transition. Every table below is created EMPTY, so every
-- benchmark quantity of every route reads unmeasured, naming the register it looked in, until a row is
-- recorded; no row is recorded in the company's own store by this change.
--
-- What it changes on delivered records, each stated so nobody has to discover it:
--   * the operation record gains a nullable column stating whether an operation's cost is stated,
--     null for every row recorded before this file, which every reader reads as NOT stated; a check,
--     binding new rows only, requires a row of the deterministic set to state its cost; and a unique
--     key over the operation, its route and its model, which an observation's reference names;
--   * the route register gains a unique key over the route and its model, for the same reference;
--   * the gate-transition record's order column, added and back-filled by the sixth resource, becomes
--     mandatory, which validates every existing row; a transition becomes write-once; and a transition
--     of an item version at an instant already recorded for that version is refused under its own name.
--
-- Reversal is by dropping what this file creates, in reverse order. It is lossless while the tables
-- it creates are empty; once an observation or a decision is recorded, restore-to-point is the
-- reversal. The mandatory order column, the write-once gate check and the same-instant check can each
-- be dropped on their own without loss.
--
-- Every persisted measurement case is ENCODED IN THE ROW SHAPE and admitted by a table check, following
-- the revenue register's discipline: an observed value carries a non-zero amount and its unit, an
-- observed zero carries its unit and NO amount, and an unmeasured quantity carries a reason from the
-- closed pair and a stated detail and no amount and no unit. A recorded amount is its own two-shape
-- case, recorded or not recorded, and is never labelled observed.

BEGIN;

-- ---------------------------------------------------------------------------
-- The shape rules, one function each, used by every table check below
-- ---------------------------------------------------------------------------

-- Exactly one of the three measurement cases. Immutable: it reads nothing but its arguments.
CREATE OR REPLACE FUNCTION measurement_case_admitted(
    the_case text, amount numeric, unit text, reason text, detail text) RETURNS boolean AS $$
    SELECT COALESCE(CASE the_case
        WHEN 'ObservedValue' THEN amount IS NOT NULL AND amount <> 0
                              AND unit IS NOT NULL AND length(btrim(unit)) > 0
                              AND reason IS NULL AND detail IS NULL
        WHEN 'ObservedZero'  THEN amount IS NULL
                              AND unit IS NOT NULL AND length(btrim(unit)) > 0
                              AND reason IS NULL AND detail IS NULL
        WHEN 'Unmeasured'    THEN amount IS NULL AND unit IS NULL
                              AND reason IN ('NoObservationExists', 'SourceCannotStateOne')
                              AND detail IS NOT NULL AND length(btrim(detail)) > 0
        ELSE false
    END, false);
$$ LANGUAGE sql IMMUTABLE;

-- Exactly one of the two recorded-amount shapes: recorded, with a positive amount, its currency and
-- the register or constant it was recorded in; or not recorded, naming what was looked for, with no
-- amount and no currency.
CREATE OR REPLACE FUNCTION recorded_amount_admitted(
    the_case text, amount numeric, currency text, recorded_in text, looked_for text) RETURNS boolean AS $$
    SELECT COALESCE(CASE the_case
        WHEN 'Recorded'    THEN amount IS NOT NULL AND amount > 0
                            AND currency IS NOT NULL AND length(btrim(currency)) > 0
                            AND recorded_in IS NOT NULL AND length(btrim(recorded_in)) > 0
                            AND looked_for IS NULL
        WHEN 'NotRecorded' THEN amount IS NULL AND currency IS NULL AND recorded_in IS NULL
                            AND looked_for IS NOT NULL AND length(btrim(looked_for)) > 0
        ELSE false
    END, false);
$$ LANGUAGE sql IMMUTABLE;

-- The closed task class set: the six representative tasks the master plan's benchmark corpus names.
CREATE OR REPLACE FUNCTION task_class_admitted(task_class text) RETURNS boolean AS $$
    SELECT COALESCE(task_class IN ('ResearchSynthesis', 'ScriptPass', 'FactCheck', 'ComplianceGatePack', 'SeoPack',
                                   'ShotListAndSceneBrief'), false);
$$ LANGUAGE sql IMMUTABLE;

-- Every row of the records below is written once. A correction in place would rewrite the evidence a
-- ranking or a decision rests on after the fact.
CREATE OR REPLACE FUNCTION refuse_economics_amendment() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'the record is written once: % on % is refused', TG_OP, TG_TABLE_NAME
        USING ERRCODE = 'integrity_constraint_violation';
END;
$$ LANGUAGE plpgsql;

-- ---------------------------------------------------------------------------
-- The operation record: whether a cost is stated, and the reference an observation names
-- ---------------------------------------------------------------------------

-- Null for every row recorded before this file, read as not stated. The recorder writes true only
-- where every consumed input, output and cached unit had a price row in force at the booking instant
-- and no other unit was consumed; otherwise false, so a missing price never reads as a stated zero.
ALTER TABLE agent_costs ADD COLUMN IF NOT EXISTS cost_stated boolean;

DO $$
BEGIN
    -- A member of the deterministic set states its cost (zero). NOT VALID: binds new rows only.
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'agent_costs_deterministic_states_cost') THEN
        ALTER TABLE agent_costs
            ADD CONSTRAINT agent_costs_deterministic_states_cost
            CHECK (deterministic_task IS NULL OR cost_stated IS TRUE) NOT VALID;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'agent_costs_operation_route_model_key') THEN
        ALTER TABLE agent_costs
            ADD CONSTRAINT agent_costs_operation_route_model_key UNIQUE (operation_id, route_id, model_id);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'routes_route_model_key') THEN
        ALTER TABLE routes ADD CONSTRAINT routes_route_model_key UNIQUE (route_id, model_id);
    END IF;
END;
$$;

-- ---------------------------------------------------------------------------
-- The benchmark record: a content-free corpus-entry register and write-once observations
-- ---------------------------------------------------------------------------

-- An entry exists only as a reference: an identifier, its task class and the datastore's registration
-- instant. There is NO CONTENT COLUMN, so no recorded input or acceptable output can be held here.
CREATE TABLE IF NOT EXISTS benchmark_corpus_entries (
    entry_id      uuid PRIMARY KEY,
    task_class    text NOT NULL CHECK (task_class_admitted(task_class)),
    registered_at timestamptz NOT NULL,
    CONSTRAINT benchmark_corpus_entries_entry_task_class UNIQUE (entry_id, task_class)
);

-- One observation per operation that ran a corpus entry. Its entry and task class are one reference,
-- so the two agree; its operation, route and model are one reference to the operation record, and its
-- route and model one reference to the route register, so the three agree with both. Its quality is
-- row-encoded in exactly one of the three cases. Its COST AND LATENCY ARE NOT COLUMNS: they are the
-- datastore's own figures for the referenced operation, read through it, so no writer supplies either.
CREATE TABLE IF NOT EXISTS benchmark_observations (
    observation_id            uuid PRIMARY KEY,
    entry_id                  uuid NOT NULL,
    task_class                text NOT NULL,
    operation_id              uuid NOT NULL UNIQUE,
    route_id                  uuid NOT NULL,
    model_id                  text NOT NULL,
    quality_case              text NOT NULL,
    quality_amount            numeric(18, 8),
    quality_unit              text,
    quality_unmeasured_reason text,
    quality_unmeasured_detail text,
    observed_at               timestamptz NOT NULL,
    period                    date NOT NULL,

    CONSTRAINT benchmark_observations_quality_case CHECK (
        measurement_case_admitted(quality_case, quality_amount, quality_unit,
                                  quality_unmeasured_reason, quality_unmeasured_detail)
    ),

    FOREIGN KEY (entry_id, task_class) REFERENCES benchmark_corpus_entries (entry_id, task_class),
    FOREIGN KEY (operation_id, route_id, model_id) REFERENCES agent_costs (operation_id, route_id, model_id),
    FOREIGN KEY (route_id, model_id) REFERENCES routes (route_id, model_id)
);

CREATE INDEX IF NOT EXISTS benchmark_observations_by_route_and_task
    ON benchmark_observations (route_id, task_class);

CREATE INDEX IF NOT EXISTS benchmark_observations_by_period
    ON benchmark_observations (period);

-- THE CHECK on a corpus entry: registered on the datastore's clock, at or above the record horizon,
-- held shared to the end of the writing transaction.
CREATE OR REPLACE FUNCTION refuse_corpus_entry_off_horizon() RETURNS trigger AS $$
DECLARE
    closed_below timestamptz;
BEGIN
    SELECT horizon INTO closed_below FROM audit_record_horizon WHERE only_row FOR SHARE;

    IF closed_below IS NOT NULL AND NEW.registered_at < closed_below THEN
        RAISE EXCEPTION 'the benchmark record is closed below %: an entry registered % is refused',
            closed_below, NEW.registered_at
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'benchmark_corpus_entries_on_horizon';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER benchmark_corpus_entries_on_horizon
    BEFORE INSERT ON benchmark_corpus_entries
    FOR EACH ROW EXECUTE FUNCTION refuse_corpus_entry_off_horizon();

CREATE OR REPLACE TRIGGER benchmark_corpus_entries_write_once
    BEFORE UPDATE OR DELETE ON benchmark_corpus_entries
    FOR EACH ROW EXECUTE FUNCTION refuse_economics_amendment();

-- THE CHECK on an observation, binding every writer: it takes the record horizon shared, held to the
-- end of the writing transaction, and refuses an observation stamped below the horizon, stamped before
-- the operation it references, or booked into any month other than its own instant's, in UTC.
CREATE OR REPLACE FUNCTION refuse_observation_off_clock() RETURNS trigger AS $$
DECLARE
    closed_below timestamptz;
    ran_at       timestamptz;
BEGIN
    SELECT horizon INTO closed_below FROM audit_record_horizon WHERE only_row FOR SHARE;

    IF closed_below IS NOT NULL AND NEW.observed_at < closed_below THEN
        RAISE EXCEPTION 'the benchmark record is closed below %: an observation stamped % is refused',
            closed_below, NEW.observed_at
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'benchmark_observations_on_clock';
    END IF;

    SELECT occurred_at INTO ran_at FROM agent_costs WHERE operation_id = NEW.operation_id;

    IF ran_at IS NOT NULL AND NEW.observed_at < ran_at THEN
        RAISE EXCEPTION 'an observation follows its operation: % stamped % precedes operation % at %',
            NEW.observation_id, NEW.observed_at, NEW.operation_id, ran_at
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'benchmark_observations_on_clock';
    END IF;

    IF NEW.period <> (date_trunc('month', NEW.observed_at AT TIME ZONE 'UTC'))::date THEN
        RAISE EXCEPTION 'an observation is booked into the month of its instant: % stamped % is not in %',
            NEW.observation_id, NEW.observed_at, NEW.period
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'benchmark_observations_on_clock';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER benchmark_observations_on_clock
    BEFORE INSERT ON benchmark_observations
    FOR EACH ROW EXECUTE FUNCTION refuse_observation_off_clock();

CREATE OR REPLACE TRIGGER benchmark_observations_write_once
    BEFORE UPDATE OR DELETE ON benchmark_observations
    FOR EACH ROW EXECUTE FUNCTION refuse_economics_amendment();

-- ---------------------------------------------------------------------------
-- The admission decision record: one header per operation, its readings, candidates and evidence
-- ---------------------------------------------------------------------------

-- One decision per admitted operation, refused, held and substituted ones included. Its instant is the
-- operation's BOOKED instant, so the decision, the operation and every reading it records are on one
-- clock and in one month.
CREATE TABLE IF NOT EXISTS admission_decisions (
    operation_id            uuid PRIMARY KEY REFERENCES agent_costs (operation_id),
    decided_at              timestamptz NOT NULL,
    booking_month           date NOT NULL,
    action                  text NOT NULL CHECK (action IN ('None', 'AlertOnly', 'Downgrade', 'Defer', 'Refuse')),
    basis                   text NOT NULL CHECK (basis IN ('Evidence', 'Configured')),
    basis_statement         text NOT NULL CHECK (length(btrim(basis_statement)) > 0),
    task_class              text CHECK (task_class IS NULL OR task_class_admitted(task_class)),
    tier_outcome            text CHECK (tier_outcome IS NULL
                                        OR tier_outcome IN ('AtRequestedTier', 'HigherTier', 'Untiered', 'Downgraded')),
    tier_statement          text NOT NULL CHECK (length(btrim(tier_statement)) > 0),
    company_basis_statement text NOT NULL CHECK (length(btrim(company_basis_statement)) > 0),
    reservation_statement   text NOT NULL CHECK (length(btrim(reservation_statement)) > 0),
    observations_ranked_on  bigint NOT NULL CHECK (observations_ranked_on >= 0)
);

-- One row per governing reading: the channel's and the company's. The amount is a recorded amount in
-- its own two shapes; booked spend and utilisation are each one of the three measurement cases.
CREATE TABLE IF NOT EXISTS admission_decision_readings (
    operation_id                  uuid NOT NULL REFERENCES admission_decisions (operation_id),
    scope_kind                    text NOT NULL CHECK (scope_kind IN ('Channel', 'Company')),

    -- Null only for the company reading where the channel register holds no company for the channel.
    scope_id                      uuid CHECK (scope_id IS NOT NULL OR scope_kind = 'Company'),
    amount_case                   text NOT NULL,
    amount                        numeric(18, 8),
    amount_currency               text,
    amount_recorded_in            text,
    amount_looked_for             text,
    spend_case                    text NOT NULL,
    spend_amount                  numeric(18, 8),
    spend_unit                    text,
    spend_unmeasured_reason       text,
    spend_unmeasured_detail       text,
    utilisation_case              text NOT NULL,
    utilisation_amount            numeric(18, 8),
    utilisation_unit              text,
    utilisation_unmeasured_reason text,
    utilisation_unmeasured_detail text,
    threshold                     integer CHECK (threshold IS NULL OR threshold IN (50, 75, 90, 100)),
    action                        text NOT NULL CHECK (action IN ('None', 'AlertOnly', 'Downgrade', 'Defer', 'Refuse')),
    reason                        text CHECK (reason IS NULL OR reason IN ('BudgetAmountNotRecorded', 'SpendUnmeasured')),
    PRIMARY KEY (operation_id, scope_kind),

    CONSTRAINT admission_decision_readings_amount_case CHECK (
        recorded_amount_admitted(amount_case, amount, amount_currency, amount_recorded_in, amount_looked_for)
    ),
    CONSTRAINT admission_decision_readings_spend_case CHECK (
        measurement_case_admitted(spend_case, spend_amount, spend_unit, spend_unmeasured_reason, spend_unmeasured_detail)
    ),
    CONSTRAINT admission_decision_readings_utilisation_case CHECK (
        measurement_case_admitted(utilisation_case, utilisation_amount, utilisation_unit,
                                  utilisation_unmeasured_reason, utilisation_unmeasured_detail)
    )
);

-- One row per candidate the selection compared, in the position the selection gave it, with its
-- configured rating, its observed quality and cost in their cases and its observation count. The
-- configured rating is a rating, never an observed quality: the two are separate columns.
CREATE TABLE IF NOT EXISTS admission_decision_candidates (
    operation_id              uuid NOT NULL REFERENCES admission_decisions (operation_id),
    route_id                  uuid NOT NULL REFERENCES routes (route_id),
    position                  integer NOT NULL CHECK (position >= 1),
    configured_rating         integer NOT NULL CHECK (configured_rating BETWEEN 0 AND 100),
    quality_case              text NOT NULL,
    quality_amount            numeric(18, 8),
    quality_unit              text,
    quality_unmeasured_reason text,
    quality_unmeasured_detail text,
    cost_case                 text NOT NULL,
    cost_amount               numeric(18, 8),
    cost_unit                 text,
    cost_unmeasured_reason    text,
    cost_unmeasured_detail    text,
    observations              bigint NOT NULL CHECK (observations >= 0),
    PRIMARY KEY (operation_id, route_id),
    CONSTRAINT admission_decision_candidates_one_per_position UNIQUE (operation_id, position),

    CONSTRAINT admission_decision_candidates_quality_case CHECK (
        measurement_case_admitted(quality_case, quality_amount, quality_unit,
                                  quality_unmeasured_reason, quality_unmeasured_detail)
    ),
    CONSTRAINT admission_decision_candidates_cost_case CHECK (
        measurement_case_admitted(cost_case, cost_amount, cost_unit, cost_unmeasured_reason, cost_unmeasured_detail)
    )
);

-- The observations an evidence ranking rested on, so its inputs read back identical.
CREATE TABLE IF NOT EXISTS admission_decision_observations (
    operation_id   uuid NOT NULL REFERENCES admission_decisions (operation_id),
    observation_id uuid NOT NULL REFERENCES benchmark_observations (observation_id),
    PRIMARY KEY (operation_id, observation_id)
);

-- THE CHECK on a decision, binding every writer: it takes the record horizon shared, held to the end
-- of the writing transaction, and refuses a decision whose instant is not its operation's booked
-- instant, whose month is not its operation's month, or which lies below the horizon.
CREATE OR REPLACE FUNCTION refuse_decision_off_operation() RETURNS trigger AS $$
DECLARE
    closed_below timestamptz;
    booked_at    timestamptz;
    booked_in    date;
BEGIN
    SELECT horizon INTO closed_below FROM audit_record_horizon WHERE only_row FOR SHARE;

    IF closed_below IS NOT NULL AND NEW.decided_at < closed_below THEN
        RAISE EXCEPTION 'the decision record is closed below %: a decision stamped % is refused',
            closed_below, NEW.decided_at
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'admission_decisions_on_operation';
    END IF;

    SELECT occurred_at, period INTO booked_at, booked_in FROM agent_costs WHERE operation_id = NEW.operation_id;

    IF booked_at IS DISTINCT FROM NEW.decided_at OR booked_in IS DISTINCT FROM NEW.booking_month THEN
        RAISE EXCEPTION 'a decision is taken at its operation''s booked instant: % stamped % in % is not % in %',
            NEW.operation_id, NEW.decided_at, NEW.booking_month, booked_at, booked_in
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'admission_decisions_on_operation';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER admission_decisions_on_operation
    BEFORE INSERT ON admission_decisions
    FOR EACH ROW EXECUTE FUNCTION refuse_decision_off_operation();

CREATE OR REPLACE TRIGGER admission_decisions_write_once
    BEFORE UPDATE OR DELETE ON admission_decisions
    FOR EACH ROW EXECUTE FUNCTION refuse_economics_amendment();

CREATE OR REPLACE TRIGGER admission_decision_readings_write_once
    BEFORE UPDATE OR DELETE ON admission_decision_readings
    FOR EACH ROW EXECUTE FUNCTION refuse_economics_amendment();

CREATE OR REPLACE TRIGGER admission_decision_candidates_write_once
    BEFORE UPDATE OR DELETE ON admission_decision_candidates
    FOR EACH ROW EXECUTE FUNCTION refuse_economics_amendment();

CREATE OR REPLACE TRIGGER admission_decision_observations_write_once
    BEFORE UPDATE OR DELETE ON admission_decision_observations
    FOR EACH ROW EXECUTE FUNCTION refuse_economics_amendment();

-- ---------------------------------------------------------------------------
-- The gate-transition record: a mandatory position, written once, one transition per instant
-- ---------------------------------------------------------------------------

-- The order column the sixth resource added and back-filled becomes MANDATORY, which validates every
-- existing row: a row without a position fails this statement loudly and names the column, and the
-- operator decides before re-applying. How many rows were validated is reported when the file is
-- applied. The sequence is not touched.
DO $$
DECLARE
    validated bigint;
BEGIN
    SELECT count(*) INTO validated FROM gate_transitions;

    ALTER TABLE gate_transitions ALTER COLUMN recorded_order SET NOT NULL;

    RAISE NOTICE 'gate transitions validated as carrying a datastore position: %', validated;
END;
$$;

-- A transition is written once. Nothing in the delivered build updates or deletes one.
CREATE OR REPLACE FUNCTION refuse_gate_transition_amendment() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'a gate transition is written once: % of item % version % is refused',
        TG_OP, OLD.item_id, OLD.item_version
        USING ERRCODE = 'integrity_constraint_violation',
              CONSTRAINT = 'gate_transitions_written_once';
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER gate_transitions_written_once
    BEFORE UPDATE OR DELETE ON gate_transitions
    FOR EACH ROW EXECUTE FUNCTION refuse_gate_transition_amendment();

-- THE SAME-INSTANT CHECK, binding every writer. A second transition of one item version at an instant
-- already recorded for that version is refused under its own name, rather than reaching the caller as
-- an unnamed key violation. It holds the item row, as the sixth resource's from-state check does (the
-- hold is already this transaction's once that check has run, so taking it again changes nothing), so
-- the first transition admitted under the hold is the one recorded.
--
-- It is its own function and its own trigger, named to fire AFTER the sixth resource's from-state check
-- on the same event, so that check's function and trigger stay exactly as the sixth resource states
-- them, and re-applying the sixth resource leaves this check in place.
CREATE OR REPLACE FUNCTION refuse_transition_at_recorded_instant() RETURNS trigger AS $$
BEGIN
    PERFORM 1 FROM items WHERE item_id = NEW.item_id FOR NO KEY UPDATE;

    IF EXISTS (
        SELECT 1 FROM gate_transitions
        WHERE item_id = NEW.item_id AND item_version = NEW.item_version AND occurred_at = NEW.occurred_at
    ) THEN
        RAISE EXCEPTION 'gate transition refused: item % version % already has a transition recorded at %',
            NEW.item_id, NEW.item_version, NEW.occurred_at
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'gate_transitions_at_recorded_instant';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER gate_transitions_same_instant_refused
    BEFORE INSERT ON gate_transitions
    FOR EACH ROW EXECUTE FUNCTION refuse_transition_at_recorded_instant();

COMMIT;
