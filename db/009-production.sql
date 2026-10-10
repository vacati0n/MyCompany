-- Wave 9, the production capability. Additive throughout, applied after the eight delivered resources.
--
-- What it creates, each EMPTY, each write-once:
--   * the store designation: one row saying whether this store is a self-dropping demonstration store or the
--     company's own store, with the database it was written in and the datastore's instant;
--   * the item cap register: one recorded cap per item, with the statement it rests on;
--   * the admission reservations: one worst-case reservation per capped provider attempt, keyed on the one
--     operation identifier the attempt is booked under, so the booking reconciles it and an attempt never
--     booked keeps counting at its worst case;
--   * the production versions: one header per production run, naming its mode and the store's designation;
--   * the production artifacts: one row per stored file, with the length and hash of the stored bytes.
--
-- What it changes on delivered records, each stated so nobody has to discover it:
--   * the price kinds admit character and image units;
--   * a model may record the unit kinds it is billed by; absent reads as input, output and cached, as delivered;
--   * a provider account may record its credential scope and authentication scheme; absent reads as delivered;
--   * a route may record its terms positions on automated access and on a licence over the customer's content,
--     with the evidence and the date it was read; absent reads not recorded;
--   * the forbidden-source kinds admit the customer-content-licence kind;
--   * the operation record gains character and image units and their applied prices, zero for every row recorded
--     before this file, and its stored cost covers all five kinds, recomputing every existing row to the value it
--     already held because the added terms are zero on it;
--   * the held-outcome reasons admit the item-cap refusal;
--   * the open-decisions register gains five decided entries, each superseding the open entry the owner's answers
--     of 2026-10-09 answered, and leaves the price-capture and register-writer entries open.
--
-- NOTHING HERE RECORDS a designation, a cap, a reservation, a channel, a budget amount, a configuration value, a
-- route, a price, a production or an artifact. The preparation command records those, from recorded
-- configuration, in a demonstration store in every phase and in the company store only on the owner's go.
--
-- Reversal is by dropping what this file creates, in reverse order, which is lossless while its tables hold only
-- demonstration rows; a company store holding a real reservation or operation is kept and never dropped.

BEGIN;

-- ---------------------------------------------------------------------------
-- The write-once refusal every record of this file shares
-- ---------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION refuse_production_amendment() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'the record is written once: % on % is refused', TG_OP, TG_TABLE_NAME
        USING ERRCODE = 'integrity_constraint_violation';
END;
$$ LANGUAGE plpgsql;

-- The datastore's instant on every row of this file: a writer's instant is never stored.
CREATE OR REPLACE FUNCTION stamp_production_row() RETURNS trigger AS $$
BEGIN
    NEW.recorded_at := clock_timestamp();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ---------------------------------------------------------------------------
-- Unit kinds, billed kinds, account scope and scheme, route terms positions
-- ---------------------------------------------------------------------------

ALTER TABLE model_prices DROP CONSTRAINT IF EXISTS model_prices_unit_kind_check;
ALTER TABLE model_prices ADD CONSTRAINT model_prices_unit_kind_check
    CHECK (unit_kind IN ('InputUnit', 'OutputUnit', 'CachedUnit', 'PerOperation', 'PerSecond', 'CharacterUnit', 'ImageUnit'));

ALTER TABLE models ADD COLUMN IF NOT EXISTS billed_kinds text[];

ALTER TABLE provider_accounts ADD COLUMN IF NOT EXISTS credential_scope text;
ALTER TABLE provider_accounts ADD COLUMN IF NOT EXISTS authentication_scheme text;

ALTER TABLE routes ADD COLUMN IF NOT EXISTS automated_access_position text;
ALTER TABLE routes ADD COLUMN IF NOT EXISTS customer_content_position text;
ALTER TABLE routes ADD COLUMN IF NOT EXISTS terms_positions_evidence text;
ALTER TABLE routes ADD COLUMN IF NOT EXISTS terms_positions_read_on date;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'models_billed_kinds_closed') THEN
        ALTER TABLE models ADD CONSTRAINT models_billed_kinds_closed CHECK (
            billed_kinds IS NULL
            OR (cardinality(billed_kinds) > 0
                AND billed_kinds <@ ARRAY['InputUnit', 'OutputUnit', 'CachedUnit', 'CharacterUnit', 'ImageUnit']::text[]));
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'provider_accounts_scope_closed') THEN
        ALTER TABLE provider_accounts ADD CONSTRAINT provider_accounts_scope_closed
            CHECK (credential_scope IS NULL OR credential_scope IN ('Company', 'Channel'));
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'provider_accounts_scheme_closed') THEN
        ALTER TABLE provider_accounts ADD CONSTRAINT provider_accounts_scheme_closed
            CHECK (authentication_scheme IS NULL OR authentication_scheme IN ('BearerAuthorization', 'KeyHeader'));
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'routes_terms_positions_closed') THEN
        ALTER TABLE routes ADD CONSTRAINT routes_terms_positions_closed CHECK (
            (automated_access_position IS NULL OR automated_access_position IN ('Permits', 'Prohibits'))
            AND (customer_content_position IS NULL OR customer_content_position IN ('NoLicenceTaken', 'LicenceTaken')));
    END IF;

    -- A recorded position names the evidence it rests on and the date that evidence was read.
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'routes_terms_positions_evidenced') THEN
        ALTER TABLE routes ADD CONSTRAINT routes_terms_positions_evidenced CHECK (
            (automated_access_position IS NULL AND customer_content_position IS NULL)
            OR (length(btrim(terms_positions_evidence)) > 0 AND terms_positions_read_on IS NOT NULL));
    END IF;
END;
$$;

ALTER TABLE forbidden_sources DROP CONSTRAINT IF EXISTS forbidden_sources_kind_check;
ALTER TABLE forbidden_sources ADD CONSTRAINT forbidden_sources_kind_check
    CHECK (kind IN ('ConsumerChatSubscription', 'MultipliedPersonalAccount', 'SharedCredential',
                    'AutomatedAccessProhibited', 'WithdrawnInterface', 'CustomerContentLicence'));

ALTER TABLE admission_held_outcomes DROP CONSTRAINT IF EXISTS admission_held_outcomes_reason_check;
ALTER TABLE admission_held_outcomes ADD CONSTRAINT admission_held_outcomes_reason_check CHECK (reason IN (
    'ForbiddenSource', 'NoAdmittedRoute', 'NoRouteAtOrAboveFloor', 'InsufficientContextCapacity',
    'NoAvailableRoute', 'CostCeilingOrBudgetExceeded', 'CurrencyMismatch', 'BudgetAmountNotRecorded',
    'SpendUnmeasured', 'DeferredAtThreshold', 'RefusedAtThreshold', 'PriceNotInForce',
    'NoRouteAtRequestedTier', 'MeteredAdmissionInProgress', 'ItemCapExceeded'));

-- ---------------------------------------------------------------------------
-- The operation record: character and image units, costed by the datastore
-- ---------------------------------------------------------------------------

ALTER TABLE agent_costs ADD COLUMN IF NOT EXISTS character_units bigint NOT NULL DEFAULT 0;
ALTER TABLE agent_costs ADD COLUMN IF NOT EXISTS image_units bigint NOT NULL DEFAULT 0;
ALTER TABLE agent_costs ADD COLUMN IF NOT EXISTS applied_character_price numeric(18, 10) NOT NULL DEFAULT 0;
ALTER TABLE agent_costs ADD COLUMN IF NOT EXISTS applied_image_price numeric(18, 10) NOT NULL DEFAULT 0;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'agent_costs_added_units_non_negative') THEN
        ALTER TABLE agent_costs ADD CONSTRAINT agent_costs_added_units_non_negative CHECK (
            character_units >= 0 AND image_units >= 0 AND applied_character_price >= 0 AND applied_image_price >= 0);
    END IF;

    -- A member of the deterministic set carries no price of the added kinds either.
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'agent_costs_deterministic_is_free_of_added_kinds') THEN
        ALTER TABLE agent_costs ADD CONSTRAINT agent_costs_deterministic_is_free_of_added_kinds CHECK (
            deterministic_task IS NULL OR (applied_character_price = 0 AND applied_image_price = 0));
    END IF;

    -- The stored cost covers all five kinds. Changing the generated expression recomputes every row; on a row
    -- recorded before this file the added units are zero, so its cost is the value it already held.
    IF NOT EXISTS (
        SELECT 1 FROM pg_attrdef d JOIN pg_attribute a ON a.attrelid = d.adrelid AND a.attnum = d.adnum
        WHERE d.adrelid = 'agent_costs'::regclass AND a.attname = 'computed_cost'
          AND pg_get_expr(d.adbin, d.adrelid) LIKE '%character_units%') THEN
        ALTER TABLE agent_costs ALTER COLUMN computed_cost SET EXPRESSION AS (
            (input_units     * applied_input_price) +
            (output_units    * applied_output_price) +
            (cached_units    * applied_cached_price) +
            (character_units * applied_character_price) +
            (image_units     * applied_image_price));
    END IF;
END;
$$;

-- ---------------------------------------------------------------------------
-- The store designation
-- ---------------------------------------------------------------------------

-- ONE ROW, written once: what this store is for. A demonstration store is self-dropping and every figure read
-- from it is labelled demonstration; the company store is the only one a metered run books into. The row
-- records the database it was written in, which must be the connected database, and the datastore's instant.
CREATE TABLE IF NOT EXISTS store_designation (
    only_row      boolean PRIMARY KEY DEFAULT true CHECK (only_row),
    designation   text NOT NULL CHECK (designation IN ('Demonstration', 'Company')),
    database_name text NOT NULL CHECK (length(btrim(database_name)) > 0),
    recorded_at   timestamptz NOT NULL
);

CREATE OR REPLACE FUNCTION refuse_designation_elsewhere() RETURNS trigger AS $$
BEGIN
    IF NEW.database_name <> current_database() THEN
        RAISE EXCEPTION 'the store designation names database %, and this is database %', NEW.database_name, current_database()
            USING ERRCODE = 'integrity_constraint_violation';
    END IF;

    NEW.recorded_at := clock_timestamp();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER store_designation_in_this_database
    BEFORE INSERT ON store_designation
    FOR EACH ROW EXECUTE FUNCTION refuse_designation_elsewhere();

CREATE OR REPLACE TRIGGER store_designation_write_once
    BEFORE UPDATE OR DELETE ON store_designation
    FOR EACH ROW EXECUTE FUNCTION refuse_production_amendment();

-- ---------------------------------------------------------------------------
-- The item cap register and the admission reservations
-- ---------------------------------------------------------------------------

-- One recorded cap per item: a configured amount with the statement it rests on, never an observation.
CREATE TABLE IF NOT EXISTS item_caps (
    item_id     uuid PRIMARY KEY REFERENCES items (item_id),
    amount      numeric(18, 8) NOT NULL CHECK (amount > 0),
    currency    text NOT NULL CHECK (length(btrim(currency)) > 0),
    source      text NOT NULL CHECK (length(btrim(source)) > 0),
    recorded_at timestamptz NOT NULL
);

CREATE OR REPLACE TRIGGER item_caps_stamped
    BEFORE INSERT ON item_caps
    FOR EACH ROW EXECUTE FUNCTION stamp_production_row();

CREATE OR REPLACE TRIGGER item_caps_write_once
    BEFORE UPDATE OR DELETE ON item_caps
    FOR EACH ROW EXECUTE FUNCTION refuse_production_amendment();

-- One worst-case reservation per capped provider attempt, keyed on the operation identifier the attempt is
-- booked under. It is written on a companion transaction committed BEFORE the call; a reservation with no
-- operation of its identifier is OPEN and counts against the item's cap at its worst case.
CREATE TABLE IF NOT EXISTS admission_reservations (
    operation_id      uuid PRIMARY KEY,
    item_id           uuid NOT NULL REFERENCES items (item_id),
    route_id          uuid NOT NULL REFERENCES routes (route_id),
    model_id          text NOT NULL REFERENCES models (model_id),
    input_units       bigint NOT NULL CHECK (input_units >= 0),
    output_units      bigint NOT NULL CHECK (output_units >= 0),
    cached_units      bigint NOT NULL CHECK (cached_units >= 0),
    character_units   bigint NOT NULL CHECK (character_units >= 0),
    image_units       bigint NOT NULL CHECK (image_units >= 0),
    worst_case_amount numeric(18, 8) NOT NULL CHECK (worst_case_amount >= 0),
    currency          text NOT NULL,
    admitted_at       timestamptz NOT NULL,
    recorded_at       timestamptz NOT NULL
);

CREATE INDEX IF NOT EXISTS admission_reservations_by_item ON admission_reservations (item_id);

CREATE OR REPLACE TRIGGER admission_reservations_stamped
    BEFORE INSERT ON admission_reservations
    FOR EACH ROW EXECUTE FUNCTION stamp_production_row();

CREATE OR REPLACE TRIGGER admission_reservations_write_once
    BEFORE UPDATE OR DELETE ON admission_reservations
    FOR EACH ROW EXECUTE FUNCTION refuse_production_amendment();

-- ---------------------------------------------------------------------------
-- Production versions and artifacts
-- ---------------------------------------------------------------------------

-- One header per production run, on the item version it opened; the dossier header of that version is written
-- in the same transaction. Its mode, and the store's designation when it opened, say what its figures are.
CREATE TABLE IF NOT EXISTS production_versions (
    item_id      uuid NOT NULL,
    item_version integer NOT NULL CHECK (item_version > 1),
    mode         text NOT NULL CHECK (mode IN ('Fake', 'Metered')),
    designation  text NOT NULL CHECK (designation IN ('Demonstration', 'Company')),
    recorded_at  timestamptz NOT NULL,
    PRIMARY KEY (item_id, item_version),
    FOREIGN KEY (item_id, item_version) REFERENCES item_dossiers (item_id, item_version)
);

CREATE OR REPLACE TRIGGER production_versions_stamped
    BEFORE INSERT ON production_versions
    FOR EACH ROW EXECUTE FUNCTION stamp_production_row();

CREATE OR REPLACE TRIGGER production_versions_write_once
    BEFORE UPDATE OR DELETE ON production_versions
    FOR EACH ROW EXECUTE FUNCTION refuse_production_amendment();

-- One row per stored file of a production, recorded only after the file was written, promoted and re-read: the
-- length and the SHA-256 of the STORED bytes, and the duration a probe of the stored file measured.
CREATE TABLE IF NOT EXISTS production_artifacts (
    item_id              uuid NOT NULL,
    item_version         integer NOT NULL,
    relative_path        text NOT NULL CHECK (length(btrim(relative_path)) > 0 AND relative_path NOT LIKE '/%' AND relative_path NOT LIKE '%..%'),
    stage                text NOT NULL CHECK (stage IN ('Design', 'Production', 'Audio', 'Thumbnail')),
    role                 text NOT NULL CHECK (role IN (
                             'GraphicStill', 'NarrationPart', 'Narration', 'ThumbnailCandidate', 'ThumbnailPlaceholder',
                             'ClipPlaceholder', 'RenderedVideo')),
    implements           text NOT NULL CHECK (length(btrim(implements)) > 0),
    length_bytes         bigint NOT NULL CHECK (length_bytes >= 0),
    sha256               text NOT NULL CHECK (sha256 ~ '^[0-9a-f]{64}$'),
    measured_duration_ms bigint CHECK (measured_duration_ms IS NULL OR measured_duration_ms >= 0),
    recorded_at          timestamptz NOT NULL,
    PRIMARY KEY (item_id, item_version, relative_path),
    FOREIGN KEY (item_id, item_version) REFERENCES production_versions (item_id, item_version)
);

CREATE OR REPLACE TRIGGER production_artifacts_stamped
    BEFORE INSERT ON production_artifacts
    FOR EACH ROW EXECUTE FUNCTION stamp_production_row();

CREATE OR REPLACE TRIGGER production_artifacts_write_once
    BEFORE UPDATE OR DELETE ON production_artifacts
    FOR EACH ROW EXECUTE FUNCTION refuse_production_amendment();

-- ---------------------------------------------------------------------------
-- The open-decisions register: the owner's answers of 2026-10-09, by supersession
-- ---------------------------------------------------------------------------

-- Each answered question is superseded by a decided entry citing the decision that answers it; the questions are
-- never edited. The price-capture entry (REG-020) and the register-writer entry (REG-021) stay open.
INSERT INTO owner_decision_register
    (entry_id, statement, kind, status, owner, recorded_on, interim_ruling, decided_by, decided_on, supersedes, entered_at)
VALUES
    ('REG-022',
     'A channel with no recorded budget amount is admitted metered work against the company metered ceiling and, for the first video, the USD 5.95 per-item cap alone; no channel budget amount is recorded or invented (CEO-D-803 item 2, research/ceo-decision-record.md).',
     'Decision', 'Decided', 'the owner', '2026-10-09', NULL, 'the owner', '2026-10-09', 'REG-004', clock_timestamp()),
    ('REG-023',
     'A route counts as priced when every unit kind its vendor bills that request by has a price in force; a unit kind the vendor never bills for that request needs no price; a billed unit kind with no price in force still refuses (CEO-D-803 item 1, research/ceo-decision-record.md).',
     'Decision', 'Decided', 'the owner', '2026-10-09', NULL, 'the owner', '2026-10-09', 'REG-007', clock_timestamp()),
    ('REG-024',
     'The orchestrator records the platform-policy re-verification results, once per wave; no day count for overdue was given, so beyond not recorded in the current wave overdue stays unmeasured (CEO-D-802 item 3, research/ceo-decision-record.md).',
     'Decision', 'Decided', 'the owner', '2026-10-09', NULL, 'the owner', '2026-10-09', 'REG-017', clock_timestamp()),
    ('REG-025',
     'The seven-item recommendation list of master plan sections 9 and 29 governs; the delivered seven-rule catalogue stands (CEO-D-802 item 2, research/ceo-decision-record.md).',
     'Decision', 'Decided', 'the owner', '2026-10-09', NULL, 'the owner', '2026-10-09', 'REG-018', clock_timestamp()),
    ('REG-026',
     'The report week is the UTC week, Monday 00:00 UTC (07:00 Vietnam time), confirmed (CEO-D-802 item 1, research/ceo-decision-record.md).',
     'Decision', 'Decided', 'the owner', '2026-10-09', NULL, 'the owner', '2026-10-09', 'REG-019', clock_timestamp())
ON CONFLICT (entry_id) DO NOTHING;

COMMIT;
