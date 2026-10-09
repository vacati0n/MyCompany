-- Wave 8, the AI-management capability. Additive throughout, with one delivered table widened.
--
-- Every object here is created only where it is absent, and every row this file inserts is inserted
-- only where its key is absent, so applying the file a second time changes nothing. It is applied after
-- every resource it depends on.
--
-- What it adds:
--   * the open-decisions register: write-once entries of the owner's decisions and open questions, a
--     change being a NEW entry naming the one it supersedes, seeded below with the transcription of the
--     decisions and open questions the company decision record and the previous wave's owner report
--     carry. No other writer of it exists in this release. An entry changes no threshold, budget,
--     configuration value, approval or control: nothing reads it except the management reports.
--   * the platform-policy statement register and the write-once re-verification result record, both
--     created EMPTY. No writer exists in this release and nothing here records a statement or a result,
--     so the re-verification line reads unmeasured until the owner directs how results are recorded.
--   * the held-outcome record: one write-once row per held admission outcome, deferrals included, with
--     its reason from the closed refusal set, the instant it escalates at, the hold timeout the request
--     declared and whether it escalates to the owner, written by the admission ledger in the admission
--     transaction beside the operation row and the decision row, at the operation's booked instant and
--     at or above the record horizon. Created EMPTY; a held operation booked before this file has no row
--     and is read as recorded before the record existed.
--
-- What it changes on a delivered record, stated so nobody has to discover it:
--   * the admission decision readings' amount, spend and utilisation columns lose their precision bound
--     (they become the unbounded decimal the readings are computed in), exactly as the seventh resource
--     widened the alert columns, so a utilisation past the old bound is recorded with the controller's
--     refusal at 100 percent rather than failing the admission transaction with an unnamed overflow.
--     Widening keeps every stored value and rewrites no row; the write-once rule is unchanged.
--
-- NOTHING HERE RECORDS a threshold, a cadence, a budget amount, a configuration value, an observation,
-- a policy statement, a re-verification result, an approval or a gate transition, and nothing here
-- reaches the company's own store until the owner authorises its install.
--
-- Reversal is by dropping what this file creates, in reverse order, and by restoring the reading columns'
-- bound, which succeeds while no stored value exceeds it; once one does, restore-to-point is the
-- reversal. The register's entries are re-created by re-applying this file.

BEGIN;

-- ---------------------------------------------------------------------------
-- The write-once refusal every record of this file shares
-- ---------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION refuse_management_amendment() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'the record is written once: % on % is refused; a change is a new entry', TG_OP, TG_TABLE_NAME
        USING ERRCODE = 'integrity_constraint_violation';
END;
$$ LANGUAGE plpgsql;

-- ---------------------------------------------------------------------------
-- The open-decisions register
-- ---------------------------------------------------------------------------

-- One row per entry. An entry is a decision or a question, open or decided as recorded; a decided entry
-- names who decided it and when. "Superseded" is never written into an entry: a later entry naming it in
-- its supersedes column is what supersedes it, so the earlier entry is never rewritten.
CREATE TABLE IF NOT EXISTS owner_decision_register (
    entry_id        text PRIMARY KEY CHECK (length(btrim(entry_id)) > 0),
    statement       text NOT NULL CHECK (length(btrim(statement)) > 0),
    kind            text NOT NULL CHECK (kind IN ('Decision', 'Question')),
    status          text NOT NULL CHECK (status IN ('Open', 'Decided')),
    owner           text NOT NULL CHECK (length(btrim(owner)) > 0),
    recorded_on     date NOT NULL,
    interim_ruling  text CHECK (interim_ruling IS NULL OR length(btrim(interim_ruling)) > 0),
    decided_by      text CHECK (decided_by IS NULL OR length(btrim(decided_by)) > 0),
    decided_on      date,
    supersedes      text REFERENCES owner_decision_register (entry_id),
    entered_at      timestamptz NOT NULL,

    -- A decided entry names its decider and date; an open one names neither.
    CONSTRAINT owner_decision_register_decided_is_named CHECK (
        (status = 'Decided' AND decided_by IS NOT NULL AND decided_on IS NOT NULL)
        OR (status = 'Open' AND decided_by IS NULL AND decided_on IS NULL)
    ),
    CONSTRAINT owner_decision_register_not_self_superseding CHECK (supersedes IS NULL OR supersedes <> entry_id)
);

-- An entry is superseded at most once, so "the entry that supersedes it" is one entry.
CREATE UNIQUE INDEX IF NOT EXISTS owner_decision_register_superseded_once
    ON owner_decision_register (supersedes) WHERE supersedes IS NOT NULL;

-- The entry instant is the datastore's, whatever a writer supplies; and a supersession naming no existing
-- entry is refused under its own name rather than as an unnamed key violation.
CREATE OR REPLACE FUNCTION stamp_register_entry() RETURNS trigger AS $$
BEGIN
    NEW.entered_at := clock_timestamp();

    IF NEW.supersedes IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM owner_decision_register WHERE entry_id = NEW.supersedes) THEN
        RAISE EXCEPTION 'register entry % supersedes %, and no such entry is recorded', NEW.entry_id, NEW.supersedes
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'owner_decision_register_supersedes_existing';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER owner_decision_register_stamped
    BEFORE INSERT ON owner_decision_register
    FOR EACH ROW EXECUTE FUNCTION stamp_register_entry();

CREATE OR REPLACE TRIGGER owner_decision_register_write_once
    BEFORE UPDATE OR DELETE ON owner_decision_register
    FOR EACH ROW EXECUTE FUNCTION refuse_management_amendment();

-- THE INITIAL TRANSCRIPTION, inserted only where absent. Each statement cites the record it is transcribed
-- from; nothing here is a new decision. The entry instant is stamped by the trigger above.
INSERT INTO owner_decision_register
    (entry_id, statement, kind, status, owner, recorded_on, interim_ruling, decided_by, decided_on, supersedes, entered_at)
VALUES
    ('REG-001',
     'No executive model call in Wave 8: the weekly recommendation-narrative pass is not authorised until the first video exists; the single-metered-operation exception stays unspent (CEO-D-700, research/ceo-decision-record.md).',
     'Decision', 'Decided', 'the owner', '2026-10-09', NULL, 'the owner', '2026-10-09', NULL, clock_timestamp()),
    ('REG-002',
     'Ten comparable runs per task before an evidence ranking replaces the labelled configured ordering; below ten the router keeps the configured ordering and the CTO report keeps its qualitative-review label; a recorded configured amount, not an observation (CEO-D-701, the figure master plan section 16 states, per CEO-C-700).',
     'Decision', 'Decided', 'the owner', '2026-10-09', NULL, 'the owner', '2026-10-09', NULL, clock_timestamp()),
    ('REG-003',
     'The company cost ceiling is the USD 34.42 metered allotment, decided by the owner as the company ceiling; the standing charge of USD 42.99 is separate and has no recorded home; the monthly budget of USD 77.41 is unchanged (CEO-D-702).',
     'Decision', 'Decided', 'the owner', '2026-10-09', NULL, 'the owner', '2026-10-09', NULL, clock_timestamp()),
    ('REG-004',
     'May a channel with no recorded budget amount rely on the company ceiling alone? (wave-7/bao-cao-ceo.md section 3.1, question 3)',
     'Question', 'Open', 'the owner', '2026-10-09',
     'No: a channel with no budget amount is refused metered work under its own reason.', NULL, NULL, NULL, clock_timestamp()),
    ('REG-005',
     'Confirm a mapping from the task complexity levels (L1 to L4) to the reasoning tiers (Light, Standard, Deep); no record relates them, so the assumed tier split cannot be tested by observation alone (wave-7/bao-cao-ceo.md section 3.1, question 4).',
     'Question', 'Open', 'the owner', '2026-10-09',
     'No mapping is recorded; every reading is per served tier and decides no split.', NULL, NULL, NULL, clock_timestamp()),
    ('REG-006',
     'Should one operation whose cost is not stated refuse all metered work until the month ends? (wave-7/bao-cao-ceo.md section 3.1, question 5)',
     'Question', 'Open', 'the owner', '2026-10-09',
     'Yes: booked spend that cannot be stated refuses metered work for the rest of the month.', NULL, NULL, NULL, clock_timestamp()),
    ('REG-007',
     'A provider with no price for cached input units is never selected for metered work: keep that, or allow it when a request uses no cached input? (wave-7/bao-cao-ceo.md section 3.1, question 6)',
     'Question', 'Open', 'the owner', '2026-10-09',
     'Never selected for metered work.', NULL, NULL, NULL, clock_timestamp()),
    ('REG-008',
     'A deferred metered request is not re-admitted; it escalates after its hold timeout. Should deferred requests be re-admitted, and how? (wave-7/bao-cao-ceo.md section 3.1, question 7)',
     'Question', 'Open', 'the owner', '2026-10-09',
     'Not re-admitted; every deferral is listed in the COO report with its reason, hold age and escalation state.', NULL, NULL, NULL, clock_timestamp()),
    ('REG-009',
     'What is channel one''s monthly budget amount? (wave-6/bao-cao-ceo.md section 3.1, question 1; carried in wave-7/bao-cao-ceo.md section 3.2)',
     'Question', 'Open', 'the owner', '2026-10-09',
     'None is recorded: the channel''s utilisation reads unmeasured and its metered work is refused under its own reason.', NULL, NULL, NULL, clock_timestamp()),
    ('REG-010',
     'Must the channels'' budget amounts together sit within the USD 77.41 monthly budget, or are they only tracked against it? (wave-6/bao-cao-ceo.md section 3.1, question 1; carried in wave-7/bao-cao-ceo.md section 3.2)',
     'Question', 'Open', 'the owner', '2026-10-09',
     'No relation is recorded; channel amounts are tracked against the company ceiling and nothing sums them.', NULL, NULL, NULL, clock_timestamp()),
    ('REG-011',
     'What are channel one''s configuration values (audience, brand, voice, schedule and the rest)? (wave-6/bao-cao-ceo.md section 3.1, question 2; carried in wave-7/bao-cao-ceo.md section 3.2)',
     'Question', 'Open', 'the owner', '2026-10-09',
     'None is recorded; the subject stays an experiment.', NULL, NULL, NULL, clock_timestamp()),
    ('REG-012',
     'Where is the standing charge of USD 42.99 per month recorded? (wave-7/bao-cao-ceo.md sections 3.1 and 3.2; CEO-D-702)',
     'Question', 'Open', 'the owner', '2026-10-09',
     'It has no recorded home; it is shown separately as a recorded amount of the envelope and never netted against metered spend.', NULL, NULL, NULL, clock_timestamp()),
    ('REG-013',
     'Does CEO-D-400 stand: no sizing, buffer, concurrency or sustainable-rate claim until a production series exists? (research/ceo-decision-record.md; wave-6/bao-cao-ceo.md; carried in wave-7/bao-cao-ceo.md section 3.2)',
     'Question', 'Open', 'the owner', '2026-10-09',
     'The orchestrator''s restrictive reading stands: no sizing claim is made.', NULL, NULL, NULL, clock_timestamp()),
    ('REG-014',
     'Does capturing the unit prices once at the reserved booking instant honour the owner''s rule that a price is re-fetched immediately before any spend, or must the booking re-fetch the price in force? (previous release record, known issue on price capture; left open at the Wave 8 Design Gate)',
     'Question', 'Open', 'the owner; the tech lead prepares the ruling', '2026-10-09',
     'Delivered behaviour stands: prices are captured at the reserved instant and applied at booking; no record states the re-fetch rule honoured.', NULL, NULL, NULL, clock_timestamp())
ON CONFLICT (entry_id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- The platform-policy statement register and the re-verification result record
-- ---------------------------------------------------------------------------

-- A statement, after master plan section 29's platform-policy entity: its platform, its label, the source
-- it is recorded from and the date it takes effect where one is stated. Created EMPTY.
CREATE TABLE IF NOT EXISTS platform_policy_statements (
    statement_id     text PRIMARY KEY CHECK (length(btrim(statement_id)) > 0),
    platform         text NOT NULL CHECK (length(btrim(platform)) > 0),
    label            text NOT NULL CHECK (length(btrim(label)) > 0),
    source_reference text NOT NULL CHECK (length(btrim(source_reference)) > 0),
    effective_on     date,
    registered_at    timestamptz NOT NULL
);

-- One result per re-verification of one statement: the date it was verified and whether it changed, with
-- the datastore's recording instant. Created EMPTY; recording a result changes no control.
CREATE TABLE IF NOT EXISTS platform_policy_reverifications (
    reverification_id uuid PRIMARY KEY,
    statement_id      text NOT NULL REFERENCES platform_policy_statements (statement_id),
    verified_on       date NOT NULL,
    changed           boolean NOT NULL,
    recorded_at       timestamptz NOT NULL
);

CREATE OR REPLACE FUNCTION stamp_policy_statement() RETURNS trigger AS $$
BEGIN
    NEW.registered_at := clock_timestamp();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION stamp_policy_reverification() RETURNS trigger AS $$
BEGIN
    NEW.recorded_at := clock_timestamp();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER platform_policy_statements_stamped
    BEFORE INSERT ON platform_policy_statements
    FOR EACH ROW EXECUTE FUNCTION stamp_policy_statement();

CREATE OR REPLACE TRIGGER platform_policy_statements_write_once
    BEFORE UPDATE OR DELETE ON platform_policy_statements
    FOR EACH ROW EXECUTE FUNCTION refuse_management_amendment();

CREATE OR REPLACE TRIGGER platform_policy_reverifications_stamped
    BEFORE INSERT ON platform_policy_reverifications
    FOR EACH ROW EXECUTE FUNCTION stamp_policy_reverification();

CREATE OR REPLACE TRIGGER platform_policy_reverifications_write_once
    BEFORE UPDATE OR DELETE ON platform_policy_reverifications
    FOR EACH ROW EXECUTE FUNCTION refuse_management_amendment();

-- ---------------------------------------------------------------------------
-- The held-outcome record
-- ---------------------------------------------------------------------------

-- One row per held admission outcome, beside its decision row. Its reason is one of the closed refusal
-- set; its escalation instant is the one the boundary computed from the held instant and the hold timeout
-- the request declared; and whether it escalates to the owner is the boundary's own flag.
CREATE TABLE IF NOT EXISTS admission_held_outcomes (
    operation_id       uuid PRIMARY KEY REFERENCES admission_decisions (operation_id),
    reason             text NOT NULL CHECK (reason IN (
                           'ForbiddenSource', 'NoAdmittedRoute', 'NoRouteAtOrAboveFloor', 'InsufficientContextCapacity',
                           'NoAvailableRoute', 'CostCeilingOrBudgetExceeded', 'CurrencyMismatch', 'BudgetAmountNotRecorded',
                           'SpendUnmeasured', 'DeferredAtThreshold', 'RefusedAtThreshold', 'PriceNotInForce',
                           'NoRouteAtRequestedTier', 'MeteredAdmissionInProgress')),
    held_at            timestamptz NOT NULL,
    escalates_at       timestamptz NOT NULL,
    hold_timeout       interval NOT NULL CHECK (hold_timeout >= interval '0'),
    escalates_to_owner boolean NOT NULL,
    CONSTRAINT admission_held_outcomes_escalates_after_held CHECK (escalates_at >= held_at)
);

-- THE CHECK, binding every writer as the decision record's does: it takes the record horizon shared, held
-- to the end of the writing transaction, and refuses a held outcome below the horizon or at any instant
-- other than its operation's booked instant.
CREATE OR REPLACE FUNCTION refuse_held_outcome_off_operation() RETURNS trigger AS $$
DECLARE
    closed_below timestamptz;
    booked_at    timestamptz;
BEGIN
    SELECT horizon INTO closed_below FROM audit_record_horizon WHERE only_row FOR SHARE;

    IF closed_below IS NOT NULL AND NEW.held_at < closed_below THEN
        RAISE EXCEPTION 'the held-outcome record is closed below %: an outcome held at % is refused', closed_below, NEW.held_at
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'admission_held_outcomes_on_operation';
    END IF;

    SELECT occurred_at INTO booked_at FROM agent_costs WHERE operation_id = NEW.operation_id;

    IF booked_at IS DISTINCT FROM NEW.held_at THEN
        RAISE EXCEPTION 'a held outcome is recorded at its operation''s booked instant: % held at % is not %',
            NEW.operation_id, NEW.held_at, booked_at
            USING ERRCODE = 'integrity_constraint_violation',
                  CONSTRAINT = 'admission_held_outcomes_on_operation';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER admission_held_outcomes_on_operation
    BEFORE INSERT ON admission_held_outcomes
    FOR EACH ROW EXECUTE FUNCTION refuse_held_outcome_off_operation();

CREATE OR REPLACE TRIGGER admission_held_outcomes_write_once
    BEFORE UPDATE OR DELETE ON admission_held_outcomes
    FOR EACH ROW EXECUTE FUNCTION refuse_management_amendment();

CREATE INDEX IF NOT EXISTS admission_held_outcomes_by_held_at ON admission_held_outcomes (held_at);

-- ---------------------------------------------------------------------------
-- The admission decision readings: the precision bound removed
-- ---------------------------------------------------------------------------

-- The utilisation is computed unbounded; a bounded column made a utilisation past ten to the tenth percent
-- fail the admission transaction with an unnamed overflow, losing a refusal's operation row and audit entry.
-- Widening keeps every stored value; the write-once triggers and the case checks are unchanged.
ALTER TABLE admission_decision_readings
    ALTER COLUMN amount TYPE numeric,
    ALTER COLUMN spend_amount TYPE numeric,
    ALTER COLUMN utilisation_amount TYPE numeric;

COMMIT;
