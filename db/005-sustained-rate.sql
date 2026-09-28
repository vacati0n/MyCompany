-- Wave 5, the capability a sustained publishing rate would need. Additive throughout.
--
-- No delivered table, column, constraint, view or trigger is altered or dropped, and every object
-- here is created only where it is absent, so applying the file to a store that already holds its
-- objects creates no duplicate. It is applied after every resource it depends on.
--
-- NOTHING HERE RECORDS A FIRST-PUBLICATION CONDITION, an approval, a gate transition or a route.
-- The installer's seed remains the closed action set only. Every table below is created EMPTY,
-- and every existing route row reads as stating no reasoning tier, which is exactly what the
-- composed path read it as before this file existed, so no route acquires a tier it never stated.
--
-- Reversal is by dropping what this file creates. It is lossless in the state this change
-- delivers, where the tables are empty and no route states a tier; once a route states a tier or a
-- dossier holds a row it is lossy, and restore-to-point becomes the reversal.
--
-- Every persisted case is ENCODED IN THE ROW SHAPE and admitted by a table check, following the
-- revenue register's discipline: no reader here has to deduce a case from a null that could mean
-- something else.

BEGIN;

-- ---------------------------------------------------------------------------
-- The stated reasoning tier on the route register
-- ---------------------------------------------------------------------------

-- Two shapes only: a route states one of the closed three tiers, or it states none. A null is the
-- same explicit absence the operation record's served tier already uses, so the encoding is one
-- rule end to end. There is NO DEFAULT: a route that never stated a tier reads as stating none,
-- never as stating a default one, and nothing derives this value from a requested tier.
ALTER TABLE routes ADD COLUMN IF NOT EXISTS reasoning_tier_stated text
    CONSTRAINT routes_reasoning_tier_stated_closed
    CHECK (reasoning_tier_stated IS NULL OR reasoning_tier_stated IN ('Light', 'Standard', 'Deep'));

-- ---------------------------------------------------------------------------
-- The item dossier register
-- ---------------------------------------------------------------------------

-- The dossier header, one row per item version, written ONCE when the dossier is opened. It is the
-- observation boundary: where it exists, a component with no row is an observed zero; where it
-- does not, every source reads unmeasured naming this register and the item version.
CREATE TABLE IF NOT EXISTS item_dossiers (
    item_id      uuid NOT NULL REFERENCES items (item_id),
    item_version integer NOT NULL,
    opened_at    timestamptz NOT NULL,
    PRIMARY KEY (item_id, item_version)
);

-- One recorded outcome per production stage per item version, from the closed twelve-stage set and
-- the closed outcome set.
CREATE TABLE IF NOT EXISTS dossier_stage_evidence (
    row_no             bigserial NOT NULL UNIQUE,
    item_id            uuid NOT NULL,
    item_version       integer NOT NULL,
    stage              text NOT NULL CHECK (stage IN (
                           'Idea', 'IdeaScoring', 'Research', 'Script', 'OriginalityCheck', 'Design',
                           'Production', 'Audio', 'Thumbnail', 'QualityCheck', 'CopyrightCheck', 'PolicyCheck')),
    outcome            text NOT NULL CHECK (outcome IN (
                           'Pending', 'Succeeded', 'Retried', 'Escalated', 'Failed', 'Held')),
    summary            text NOT NULL,
    recorded_at        timestamptz NOT NULL,
    evidence_reference text,
    PRIMARY KEY (item_id, item_version, stage),
    FOREIGN KEY (item_id, item_version) REFERENCES item_dossiers (item_id, item_version)
);

-- One row per recorded supply-audit entry. THE COUNT IS HELD EXACTLY AS THE ENTRY HOLDS IT: a zero
-- as zero and an absent count as absent, which are different facts. A present count carries no
-- unobtained reason, and a prior observation is recorded whole or not at all.
CREATE TABLE IF NOT EXISTS dossier_supply_audit_entries (
    row_no               bigserial PRIMARY KEY,
    item_id              uuid NOT NULL,
    item_version         integer NOT NULL,
    requested_term       text NOT NULL,
    library              text NOT NULL,
    fidelity             text NOT NULL CHECK (fidelity IN (
                             'Unknown', 'LiteralSingleToken', 'Substituted', 'SpellingCorrected', 'MultiWordPhrase')),
    audited_at           timestamptz NOT NULL,
    term_answered        text,
    clip_count           integer CHECK (clip_count IS NULL OR clip_count >= 0),
    unobtained_reason    text NOT NULL CHECK (unobtained_reason IN (
                             'None', 'TermNotAnsweredLiterally', 'LibrarySurfaceUnreachable',
                             'RequiresPerClipConfirmation', 'LibraryReportsNone')),
    what_would_obtain_it text,
    prior_reported_total integer,
    prior_observed_on    date,
    prior_source         text,
    prior_fidelity       text CHECK (prior_fidelity IS NULL OR prior_fidelity IN (
                             'Unknown', 'LiteralSingleToken', 'Substituted', 'SpellingCorrected', 'MultiWordPhrase')),
    recorded_unavailable boolean NOT NULL,

    CONSTRAINT dossier_supply_audit_present_count_has_no_unobtained_reason CHECK (
        clip_count IS NULL OR unobtained_reason = 'None'
    ),

    CONSTRAINT dossier_supply_audit_prior_is_whole CHECK (
        (prior_reported_total IS NULL AND prior_observed_on IS NULL AND prior_source IS NULL AND prior_fidelity IS NULL)
        OR
        (prior_reported_total IS NOT NULL AND prior_observed_on IS NOT NULL AND prior_source IS NOT NULL
             AND prior_fidelity IS NOT NULL)
    ),

    FOREIGN KEY (item_id, item_version) REFERENCES item_dossiers (item_id, item_version)
);

-- One resolution per member of the closed five-member determination set per item version. The
-- row carries the fields its outcome requires: an evidenced outcome its evidence, a not-evidenceable
-- one its reason and its remedy, and only a not-evidenceable outcome carries a reason or a remedy.
-- Evidence is admitted beside every outcome, because the delivered resolution composition names the
-- evidence a used surface produced beside a not-evidenceable or failed outcome rather than discarding it.
CREATE TABLE IF NOT EXISTS dossier_determinations (
    row_no                          bigserial NOT NULL UNIQUE,
    item_id                         uuid NOT NULL,
    item_version                    integer NOT NULL,
    determination                   text NOT NULL CHECK (determination IN (
                                        'OriginalitySelfAssessment', 'DuplicateDetection', 'SyntheticMediaDisclosure',
                                        'AffiliateAndPaidPromotion', 'AdvertiserSuitability')),
    outcome                         text NOT NULL CHECK (outcome IN (
                                        'Unresolved', 'Evidenced', 'RecordedNotEvidenceable', 'Failed')),
    resolved_at                     timestamptz NOT NULL,
    evidence                        text,
    not_evidenceable_reason         text,
    what_would_make_it_evidenceable text,
    policy_reference                text,
    policy_verified_on              date,
    PRIMARY KEY (item_id, item_version, determination),

    CONSTRAINT dossier_determinations_outcome_carries_its_fields CHECK (
        (outcome <> 'Evidenced' OR (evidence IS NOT NULL AND length(btrim(evidence)) > 0))
        AND
        (outcome <> 'RecordedNotEvidenceable'
             OR (not_evidenceable_reason IS NOT NULL AND length(btrim(not_evidenceable_reason)) > 0
                 AND what_would_make_it_evidenceable IS NOT NULL
                 AND length(btrim(what_would_make_it_evidenceable)) > 0))
        AND
        (outcome = 'RecordedNotEvidenceable'
             OR (not_evidenceable_reason IS NULL AND what_would_make_it_evidenceable IS NULL))
    ),

    FOREIGN KEY (item_id, item_version) REFERENCES item_dossiers (item_id, item_version)
);

-- The dossier components no reading takes a quantity from, one row per recorded component holding
-- one structured payload. The four singleton kinds are unique per item version.
CREATE TABLE IF NOT EXISTS dossier_components (
    row_no       bigserial PRIMARY KEY,
    item_id      uuid NOT NULL,
    item_version integer NOT NULL,
    kind         text NOT NULL CHECK (kind IN (
                     'TreatmentVerdict', 'AudienceDesignation', 'VisualProvenance', 'ClipOriginAssessment',
                     'ClaimAttribution', 'ItemMetadata', 'OriginalityAssessment', 'Runtime')),
    recorded_at  timestamptz NOT NULL,
    payload      jsonb NOT NULL CHECK (jsonb_typeof(payload) = 'object' AND payload <> '{}'::jsonb),
    FOREIGN KEY (item_id, item_version) REFERENCES item_dossiers (item_id, item_version)
);

CREATE UNIQUE INDEX IF NOT EXISTS dossier_components_one_singleton_per_version
    ON dossier_components (item_id, item_version, kind)
    WHERE kind IN ('AudienceDesignation', 'ItemMetadata', 'OriginalityAssessment', 'Runtime');

-- Every dossier row is written once. A correction in place would rewrite the evidence a reading
-- rests on after the fact, so an update or a delete is refused here, as the dispatch record's is.
CREATE OR REPLACE FUNCTION refuse_dossier_amendment() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'the item dossier is written once: % on % is refused', TG_OP, TG_TABLE_NAME
        USING ERRCODE = 'integrity_constraint_violation';
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER item_dossiers_write_once
    BEFORE UPDATE OR DELETE ON item_dossiers
    FOR EACH ROW EXECUTE FUNCTION refuse_dossier_amendment();

CREATE OR REPLACE TRIGGER dossier_stage_evidence_write_once
    BEFORE UPDATE OR DELETE ON dossier_stage_evidence
    FOR EACH ROW EXECUTE FUNCTION refuse_dossier_amendment();

CREATE OR REPLACE TRIGGER dossier_supply_audit_entries_write_once
    BEFORE UPDATE OR DELETE ON dossier_supply_audit_entries
    FOR EACH ROW EXECUTE FUNCTION refuse_dossier_amendment();

CREATE OR REPLACE TRIGGER dossier_determinations_write_once
    BEFORE UPDATE OR DELETE ON dossier_determinations
    FOR EACH ROW EXECUTE FUNCTION refuse_dossier_amendment();

CREATE OR REPLACE TRIGGER dossier_components_write_once
    BEFORE UPDATE OR DELETE ON dossier_components
    FOR EACH ROW EXECUTE FUNCTION refuse_dossier_amendment();

-- ---------------------------------------------------------------------------
-- The throughput reading's access path
-- ---------------------------------------------------------------------------

-- The throughput reading is the first reader of the append-only record by action and instant,
-- which the delivered index by subject and instant does not serve. It reads those two columns and
-- no other.
CREATE INDEX IF NOT EXISTS audit_entries_by_action ON audit_entries (action, occurred_at);

COMMIT;
