-- Media company wave 3 — the publishing capability, built short of upload.
--
-- Three additions and one constraint, all forward in direction and all reversible while no row
-- exists. Every change here is ADDITIVE: no delivered column is dropped, narrowed or retyped, so a
-- reader written against the delivered shape continues to resolve.
--
-- The property this file exists for is exactly-once publication dispatch. It is a DATASTORE
-- property, not an application-side reconciliation: the uniqueness over (item_id, item_version)
-- below is what makes a duplicate dispatch IMPOSSIBLE rather than detectable, and the dispatch
-- row, the queue entry and the gate state change are written inside one transaction, so an
-- interruption between them leaves none of the three rather than one.
--
-- Nothing here records a publication. There is no published_at, no remote identifier and no
-- delivery state, because no component in this build effects a dispatch; the furthest the system
-- reaches is a composed descriptor at rest, which is what publication_dispatches holds.

BEGIN;

-- ---------------------------------------------------------------------------
-- The three conditions of first publication (module M-024)
-- ---------------------------------------------------------------------------

-- A three-valued observation. 'Unknown' is a RECORDED value, distinct from the absence of a row:
-- both fold to not-satisfied at evaluation, and the application keeps them apart in the refusal
-- so a reader can tell an unverifiable condition from one observed to be false.
--
-- Before this table exists a reader finds no observation, which folds to unknown and therefore to
-- not satisfied, so the gate is strictly MORE refusing before the migration than after it. That
-- is why the direction is safe in both orders.
CREATE TABLE first_publication_conditions (
    channel_id   uuid NOT NULL REFERENCES channels (channel_id),
    condition    text NOT NULL CHECK (condition IN (
                     'LibraryRegistration', 'PaymentAccount', 'TwoStepVerification')),
    state        text NOT NULL CHECK (state IN ('Unknown', 'NotSatisfied', 'Satisfied')),

    -- An observation with no evidence is not one. The application refuses it at construction and
    -- the datastore refuses it here, so neither route admits an unevidenced condition.
    evidence     text NOT NULL CHECK (length(btrim(evidence)) > 0),
    observed_on  date NOT NULL,

    PRIMARY KEY (channel_id, condition, observed_on)
);

CREATE INDEX first_publication_conditions_current
    ON first_publication_conditions (channel_id, condition, observed_on DESC);

-- ---------------------------------------------------------------------------
-- The dispatch record (module M-023)
-- ---------------------------------------------------------------------------

-- EXACTLY-ONCE. The primary key is the item and its exact version, so a second dispatch record
-- for one item version cannot be written whatever the application does. The dispatch key is
-- derived from the same pair rather than generated per attempt, and it is carried as its own
-- unique column so a retry resolving the key arrives at the existing row.
CREATE TABLE publication_dispatches (
    item_id           uuid NOT NULL REFERENCES items (item_id),
    item_version      integer NOT NULL,

    -- Derived from (item_id, item_version) alone. Unique in its own right, so the two ways of
    -- identifying a dispatch cannot disagree.
    dispatch_key      text NOT NULL UNIQUE CHECK (length(dispatch_key) = 64),

    destination       text NOT NULL CHECK (length(btrim(destination)) > 0),

    -- An attribute of the descriptor. NOTHING READS IT AS A DUE TIME: no component in this build
    -- selects rows by planned_at, so an elapsed planned time has nothing to trigger.
    planned_at        timestamptz NOT NULL,

    metadata_digest   text NOT NULL CHECK (length(metadata_digest) = 64),
    composed_at       timestamptz NOT NULL,

    PRIMARY KEY (item_id, item_version)
);

-- ---------------------------------------------------------------------------
-- The attempt record (module M-023) — refused attempts included
-- ---------------------------------------------------------------------------

-- Created together with the dispatch table so no attempt can reference an absent dispatch.
--
-- The five audit answers are COLUMNS rather than a reconstruction over the append-only record,
-- because the question is asked years later and a reconstruction depends on the configuration
-- that produced it still being readable. A refused attempt carries all five exactly as an
-- unrefused one does.
CREATE TABLE publication_attempts (
    attempt_id       uuid PRIMARY KEY,
    dispatch_key     text NOT NULL,

    -- Answer one: what item, and which exact version.
    item_id          uuid NOT NULL REFERENCES items (item_id),
    item_version     integer NOT NULL,

    -- Answer two: to which destination.
    destination      text NOT NULL CHECK (length(btrim(destination)) > 0),

    -- Answer three: when.
    attempted_at     timestamptz NOT NULL,

    -- Answer four: with which metadata and settings.
    metadata_digest  text NOT NULL CHECK (length(metadata_digest) = 64),

    -- Answer five: on whose approval.
    approved_by      text NOT NULL CHECK (length(btrim(approved_by)) > 0),

    refused          boolean NOT NULL,
    refusal_reason   text,
    refusal_detail   text,

    -- A refused attempt names the condition it failed. An unnamed refusal is not recordable.
    CONSTRAINT publication_attempts_refusal_is_named CHECK (
        NOT refused OR (refusal_reason IS NOT NULL AND length(btrim(refusal_reason)) > 0)
    )
);

CREATE INDEX publication_attempts_by_item ON publication_attempts (item_id, item_version, attempted_at);
CREATE INDEX publication_attempts_by_key  ON publication_attempts (dispatch_key, attempted_at);

-- A dispatch row must not be amended in place: the composed descriptor is the evidence the
-- exercise rests on, and an in-place correction would rewrite it after the fact.
CREATE FUNCTION refuse_dispatch_amendment() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'a publication dispatch is written once: % on item % version % is refused',
        TG_OP, OLD.item_id, OLD.item_version
        USING ERRCODE = 'integrity_constraint_violation';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER publication_dispatches_write_once
    BEFORE UPDATE OR DELETE ON publication_dispatches
    FOR EACH ROW EXECUTE FUNCTION refuse_dispatch_amendment();

-- ---------------------------------------------------------------------------
-- The approval marks (module M-027)
-- ---------------------------------------------------------------------------

-- Two nullable columns, so the delivered approval row is unchanged and still constructs. A null
-- resolves as UNMEASURED at the measurement level rather than as zero, which is where the
-- obligation binds; recording an unknown queue interval as zero would understate the cost of
-- per-publication approval and argue for relaxing it on the wrong figure.
ALTER TABLE approvals ADD COLUMN queued_at timestamptz;
ALTER TABLE approvals ADD COLUMN rework_of timestamptz;

-- The queued mark precedes the presentation whenever it exists. A genuine zero wait is recorded
-- as an equal instant, which is a different value from null and means a different thing.
ALTER TABLE approvals ADD CONSTRAINT approvals_queued_before_presented
    CHECK (queued_at IS NULL OR queued_at <= presented_at);

-- The three quantities, derived in the datastore from the recorded marks rather than entered.
-- Review and queue are GENERATED; rework needs the predecessor row and is therefore a view below.
-- Each is null exactly when its mark is absent, which is what carries the unmeasured resolution
-- through to any reader.
ALTER TABLE approvals ADD COLUMN queue_minutes integer
    GENERATED ALWAYS AS (
        CASE WHEN queued_at IS NULL THEN NULL
             ELSE GREATEST(0, (EXTRACT(EPOCH FROM (presented_at - queued_at)) / 60)::integer)
        END
    ) STORED;

-- Review, queue and rework as three separate quantities per approval, with rework resolved
-- through the predecessor the rework link names. An approval missing any one quantity yields
-- NULL for that quantity and 'Unmeasured' for the whole row: the view never substitutes a zero
-- for an interval nobody measured.
CREATE VIEW v_approval_measurements AS
SELECT
    a.item_id,
    a.item_version,
    a.gate,
    a.approver,
    a.presented_at,
    a.elapsed_minutes                            AS review_minutes,
    a.queue_minutes,
    CASE
        WHEN a.rework_of IS NULL THEN 0
        WHEN prior.decided_at IS NULL THEN NULL
        ELSE GREATEST(0, (EXTRACT(EPOCH FROM (a.presented_at - prior.decided_at)) / 60)::integer)
    END                                          AS rework_minutes,
    CASE
        WHEN a.queue_minutes IS NULL THEN 'Unmeasured'
        WHEN a.rework_of IS NOT NULL AND prior.decided_at IS NULL THEN 'Unmeasured'
        ELSE 'Measured'
    END                                          AS resolution
FROM approvals a
LEFT JOIN approvals prior
       ON prior.item_id  = a.item_id
      AND prior.gate     = a.gate
      AND prior.approver = a.approver
      AND prior.presented_at = a.rework_of;

-- ---------------------------------------------------------------------------
-- The served reasoning tier (module M-002 through M-005)
-- ---------------------------------------------------------------------------

-- Both columns are nullable, and a null served tier is the EXPLICIT ABSENCE MARKER: it records
-- that the admitting route could not state one, never that the tier is unknown or standard. The
-- served value is written from the admitted route at the resolution boundary and is never copied
-- from the request.
ALTER TABLE agent_costs ADD COLUMN reasoning_tier_requested text
    CHECK (reasoning_tier_requested IS NULL OR reasoning_tier_requested IN ('Light', 'Standard', 'Deep'));
ALTER TABLE agent_costs ADD COLUMN reasoning_tier_served text
    CHECK (reasoning_tier_served IS NULL OR reasoning_tier_served IN ('Light', 'Standard', 'Deep'));

-- The served-tier count, reported as a MEASURED count that may legitimately be zero. A zero here
-- means the resolution boundary recorded no operation whose admitting route stated a tier; it
-- does not mean the split is unknown-but-assumed.
CREATE VIEW v_served_tier_evidence AS
SELECT
    period,
    COUNT(*)                                                       AS operations,
    COUNT(*) FILTER (WHERE reasoning_tier_served IS NOT NULL)      AS with_served_tier,
    COUNT(*) FILTER (WHERE reasoning_tier_requested IS NOT NULL
                       AND reasoning_tier_served IS NOT NULL)      AS carrying_tier_evidence
FROM agent_costs
GROUP BY period;

COMMIT;
