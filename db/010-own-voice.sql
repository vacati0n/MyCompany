-- Wave 10, the company's own voice. Additive throughout, applied after the nine delivered resources, and applicable ALONE
-- to a store that already holds them (the install command's --from 10).
--
-- What it creates, each EMPTY, each write-once and stamped by the datastore's own function:
--   * the recording registrations: one row per registration of an item's package version, with the performer's name and the
--     release record's four other fields, each NULL when it was not given — nothing here is ever filled in;
--   * the registered beat files: one row per beat of a registration, with the stored copy's path under the output root, the
--     SHA-256 and length of the stored bytes, and what a decode of them measured;
--   * the narration measurements: one row per narration artifact, with what a decode of it measured and, for a beat, the
--     words of its text and the duration expected at the script's own rate — a reported reference, never a target;
--   * the narration provenance: one row per own-source narration part, the in-house model's identity, licences, settings,
--     file hashes, argument list and repeatability, or the recording's registration, beat, performer and release.
--
-- Every DURATION here is a generated column, the decoded sample count over the decoded sample rate, so no writer can record a
-- duration read from a container's header. No column holds a document, a recording or a model file: no binary column exists.
--
-- What it changes on delivered records, each stated so nobody has to discover it:
--   * a production version's mode admits the own mode;
--   * a production version may record its narration source, one of the four, consistent with its mode; NULL on every row
--     recorded before this file, which a reader reads as its mode's source (fake: Fake; metered: Vendor).
--
-- NOTHING HERE RECORDS a registration, a measurement, a provenance, a production, an expected hash or a setting. Reversal is
-- by dropping what this file creates, in reverse order, while its tables hold only demonstration rows; a company store
-- holding a real registration, measurement or provenance is restored from the backup taken before it was applied.

BEGIN;

-- ---------------------------------------------------------------------------
-- The production version: the own mode and the narration source
-- ---------------------------------------------------------------------------

ALTER TABLE production_versions DROP CONSTRAINT IF EXISTS production_versions_mode_check;
ALTER TABLE production_versions ADD CONSTRAINT production_versions_mode_check CHECK (mode IN ('Fake', 'Metered', 'Own'));

ALTER TABLE production_versions ADD COLUMN IF NOT EXISTS narration_source text;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'production_versions_narration_source_closed') THEN
        ALTER TABLE production_versions ADD CONSTRAINT production_versions_narration_source_closed CHECK (
            narration_source IS NULL OR narration_source IN ('Recording', 'InHouseModel', 'Fake', 'Vendor'));
    END IF;

    -- The source agrees with the mode: the own mode narrates from a recording or the in-house model, fake mode from the fake,
    -- metered mode from the vendor; a row recorded before this file carries no source.
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'production_versions_source_matches_mode') THEN
        ALTER TABLE production_versions ADD CONSTRAINT production_versions_source_matches_mode CHECK (
            (mode = 'Own' AND narration_source IN ('Recording', 'InHouseModel'))
            OR (mode = 'Fake' AND (narration_source IS NULL OR narration_source = 'Fake'))
            OR (mode = 'Metered' AND (narration_source IS NULL OR narration_source = 'Vendor')));
    END IF;
END;
$$;

-- ---------------------------------------------------------------------------
-- Recording registrations and their beat files
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS recording_registrations (
    registration_id            uuid PRIMARY KEY,
    item_id                    uuid NOT NULL REFERENCES items (item_id),
    package_version            integer NOT NULL CHECK (package_version >= 1),
    performer_name             text CHECK (performer_name IS NULL OR length(btrim(performer_name)) > 0),
    release_document_reference text CHECK (release_document_reference IS NULL OR length(btrim(release_document_reference)) > 0),
    release_document_date      date,
    release_document_sha256    text CHECK (release_document_sha256 IS NULL OR release_document_sha256 ~ '^[0-9a-f]{64}$'),
    release_training_term      text CHECK (release_training_term IS NULL OR length(btrim(release_training_term)) > 0),
    recorded_at                timestamptz NOT NULL
);

CREATE INDEX IF NOT EXISTS recording_registrations_by_item ON recording_registrations (item_id, package_version, recorded_at);

CREATE OR REPLACE TRIGGER recording_registrations_stamped
    BEFORE INSERT ON recording_registrations
    FOR EACH ROW EXECUTE FUNCTION stamp_production_row();

CREATE OR REPLACE TRIGGER recording_registrations_write_once
    BEFORE UPDATE OR DELETE ON recording_registrations
    FOR EACH ROW EXECUTE FUNCTION refuse_production_amendment();

CREATE TABLE IF NOT EXISTS registered_beat_files (
    registration_id         uuid NOT NULL REFERENCES recording_registrations (registration_id),
    beat                    integer NOT NULL CHECK (beat >= 1),
    stored_path             text NOT NULL CHECK (length(btrim(stored_path)) > 0 AND stored_path NOT LIKE '/%' AND stored_path NOT LIKE '%..%'),
    length_bytes            bigint NOT NULL CHECK (length_bytes >= 0),
    sha256                  text NOT NULL CHECK (sha256 ~ '^[0-9a-f]{64}$'),
    container               text NOT NULL CHECK (length(btrim(container)) > 0),
    codec                   text NOT NULL CHECK (length(btrim(codec)) > 0),
    sample_rate             integer NOT NULL CHECK (sample_rate > 0),
    channels                integer NOT NULL CHECK (channels > 0),
    sample_format           text NOT NULL CHECK (length(btrim(sample_format)) > 0),
    decoded_samples         bigint NOT NULL CHECK (decoded_samples >= 0),
    duration_seconds        numeric GENERATED ALWAYS AS (decoded_samples::numeric / sample_rate) STORED,
    duration_basis          text NOT NULL CHECK (duration_basis = 'decoded sample count over sample rate'),
    loudness_lufs           numeric(8, 2),
    loudness_measure        text NOT NULL CHECK (length(btrim(loudness_measure)) > 0),
    loudness_unit           text NOT NULL CHECK (loudness_unit = 'LUFS'),
    loudness_not_measurable text,
    recorded_at             timestamptz NOT NULL,
    PRIMARY KEY (registration_id, beat),
    CHECK ((loudness_lufs IS NULL) <> (loudness_not_measurable IS NULL))
);

CREATE OR REPLACE TRIGGER registered_beat_files_stamped
    BEFORE INSERT ON registered_beat_files
    FOR EACH ROW EXECUTE FUNCTION stamp_production_row();

CREATE OR REPLACE TRIGGER registered_beat_files_write_once
    BEFORE UPDATE OR DELETE ON registered_beat_files
    FOR EACH ROW EXECUTE FUNCTION refuse_production_amendment();

-- ---------------------------------------------------------------------------
-- Narration measurements and narration provenance, one per narration artifact
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS narration_measurements (
    item_id                 uuid NOT NULL,
    item_version            integer NOT NULL,
    relative_path           text NOT NULL,
    container               text NOT NULL CHECK (length(btrim(container)) > 0),
    codec                   text NOT NULL CHECK (length(btrim(codec)) > 0),
    sample_rate             integer NOT NULL CHECK (sample_rate > 0),
    channels                integer NOT NULL CHECK (channels > 0),
    sample_format           text NOT NULL CHECK (length(btrim(sample_format)) > 0),
    decoded_samples         bigint NOT NULL CHECK (decoded_samples >= 0),
    duration_seconds        numeric GENERATED ALWAYS AS (decoded_samples::numeric / sample_rate) STORED,
    duration_basis          text NOT NULL CHECK (duration_basis = 'decoded sample count over sample rate'),
    loudness_lufs           numeric(8, 2),
    loudness_measure        text NOT NULL CHECK (length(btrim(loudness_measure)) > 0),
    loudness_unit           text NOT NULL CHECK (loudness_unit = 'LUFS'),
    loudness_not_measurable text,
    beat                    integer CHECK (beat IS NULL OR beat >= 1),
    words                   integer CHECK (words IS NULL OR words >= 0),
    expected_seconds        numeric(12, 1) CHECK (expected_seconds IS NULL OR expected_seconds >= 0),
    expectation_basis       text NOT NULL CHECK (length(btrim(expectation_basis)) > 0),
    recorded_at             timestamptz NOT NULL,
    PRIMARY KEY (item_id, item_version, relative_path),
    FOREIGN KEY (item_id, item_version, relative_path) REFERENCES production_artifacts (item_id, item_version, relative_path),
    CHECK ((loudness_lufs IS NULL) <> (loudness_not_measurable IS NULL))
);

CREATE OR REPLACE TRIGGER narration_measurements_stamped
    BEFORE INSERT ON narration_measurements
    FOR EACH ROW EXECUTE FUNCTION stamp_production_row();

CREATE OR REPLACE TRIGGER narration_measurements_write_once
    BEFORE UPDATE OR DELETE ON narration_measurements
    FOR EACH ROW EXECUTE FUNCTION refuse_production_amendment();

CREATE TABLE IF NOT EXISTS narration_provenance (
    item_id                 uuid NOT NULL,
    item_version            integer NOT NULL,
    relative_path           text NOT NULL,
    narration_source        text NOT NULL CHECK (narration_source IN ('Recording', 'InHouseModel')),
    model_name              text,
    model_version           text,
    code_licence            text,
    voice_name              text,
    dataset_licence         text,
    weights_licence         text,
    voice_licence           text,
    generation_settings     text,
    file_hashes             text,
    record_entries_verified integer CHECK (record_entries_verified IS NULL OR record_entries_verified >= 0),
    arguments               text,
    repeatability           text CHECK (repeatability IS NULL OR repeatability LIKE 'repeats%' OR repeatability LIKE 'not observed to repeat%'),
    registration_id         uuid REFERENCES recording_registrations (registration_id),
    beat                    integer CHECK (beat IS NULL OR beat >= 1),
    performer_name          text,
    release_statement       text,
    recorded_at             timestamptz NOT NULL,
    PRIMARY KEY (item_id, item_version, relative_path),
    FOREIGN KEY (item_id, item_version, relative_path) REFERENCES production_artifacts (item_id, item_version, relative_path),
    -- Every field of a model part is present; a recording part names its registration, beat and release statement.
    CHECK (narration_source <> 'InHouseModel' OR (
        model_name IS NOT NULL AND model_version IS NOT NULL AND code_licence IS NOT NULL AND voice_name IS NOT NULL
        AND dataset_licence IS NOT NULL AND weights_licence IS NOT NULL AND voice_licence IS NOT NULL AND generation_settings IS NOT NULL
        AND file_hashes IS NOT NULL AND record_entries_verified IS NOT NULL AND arguments IS NOT NULL AND repeatability IS NOT NULL)),
    CHECK (narration_source <> 'Recording' OR (registration_id IS NOT NULL AND beat IS NOT NULL AND release_statement IS NOT NULL))
);

CREATE OR REPLACE TRIGGER narration_provenance_stamped
    BEFORE INSERT ON narration_provenance
    FOR EACH ROW EXECUTE FUNCTION stamp_production_row();

CREATE OR REPLACE TRIGGER narration_provenance_write_once
    BEFORE UPDATE OR DELETE ON narration_provenance
    FOR EACH ROW EXECUTE FUNCTION refuse_production_amendment();

COMMIT;
