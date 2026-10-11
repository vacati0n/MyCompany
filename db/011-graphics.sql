-- Wave 11, graphics drawn by code. Additive throughout, applied after the ten delivered resources, and applicable ALONE to a
-- store that already holds them (the install command's --from 11).
--
-- What it changes on a delivered record, stated so nobody has to discover it:
--   * the production artifact's role admits four more values: GraphicMotion (a motion graphic's frame manifest), ContactSheet,
--     ContactSheetFrame (a graphic's final frame copied byte for byte beside the sheet) and GraphicPreview. The seven delivered
--     values keep their meaning; no delivered row, key or other check changes.
--
-- What it creates, EMPTY, write-once and stamped by the datastore's own function:
--   * the graphic provenance: one row per graphic artifact, on the artifact's own transaction, keyed on the artifact's item,
--     version and relative path, naming the specification, style, drawing program, render script and bundle by their hashes;
--     the renderer, browser, Node and face identities read from verified bytes; the four manifests' hashes and file counts; the
--     segment and the changing frames; the final-frame and frame-set digests; the concurrency, argument list and request hash;
--     the render seconds observed on this machine; the processes left in the job; and the sameness kind and statement, made
--     only from compared bytes. Every hash column is lowercase hexadecimal; no column holds an image, a frame or a font.
--
-- NOTHING HERE RECORDS a graphic, a provenance, an approval, a setting or an expected hash. Re-applying it changes nothing.
-- Reversal, while only demonstration rows use the new values: drop the graphic provenance table, then restore the role check
-- to the seven delivered values; a company store holding a graphics row is restored from the backup taken before it was applied.

BEGIN;

-- ---------------------------------------------------------------------------
-- The artifact role: four more values, the seven delivered ones unchanged
-- ---------------------------------------------------------------------------

ALTER TABLE production_artifacts DROP CONSTRAINT IF EXISTS production_artifacts_role_check;
ALTER TABLE production_artifacts ADD CONSTRAINT production_artifacts_role_check CHECK (role IN (
    'GraphicStill', 'NarrationPart', 'Narration', 'ThumbnailCandidate', 'ThumbnailPlaceholder', 'ClipPlaceholder', 'RenderedVideo',
    'GraphicMotion', 'ContactSheet', 'ContactSheetFrame', 'GraphicPreview'));

-- ---------------------------------------------------------------------------
-- The graphic provenance
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS graphic_provenance (
    item_id                 uuid NOT NULL,
    item_version            integer NOT NULL,
    relative_path           text NOT NULL,
    position                text NOT NULL CHECK (length(btrim(position)) > 0),
    kind                    text NOT NULL CHECK (kind IN ('Still', 'Motion')),
    specification_path      text NOT NULL CHECK (length(btrim(specification_path)) > 0),
    specification_sha256    text NOT NULL CHECK (specification_sha256 ~ '^[0-9a-f]{64}$'),
    style_sha256            text NOT NULL CHECK (style_sha256 ~ '^[0-9a-f]{64}$'),
    drawing_program_digest  text NOT NULL CHECK (drawing_program_digest ~ '^[0-9a-f]{64}$'),
    render_script_sha256    text NOT NULL CHECK (render_script_sha256 ~ '^[0-9a-f]{64}$'),
    bundle_digest           text NOT NULL CHECK (bundle_digest ~ '^[0-9a-f]{64}$'),
    renderer_name           text NOT NULL,
    renderer_version        text NOT NULL,
    manifests               text NOT NULL,
    node_sha256             text NOT NULL CHECK (node_sha256 ~ '^[0-9a-f]{64}$'),
    node_version            text NOT NULL,
    browser_sha256          text NOT NULL CHECK (browser_sha256 ~ '^[0-9a-f]{64}$'),
    browser_version         text NOT NULL,
    faces                   text NOT NULL,
    segment_start_frame     bigint NOT NULL CHECK (segment_start_frame >= 0),
    segment_frames          bigint NOT NULL CHECK (segment_frames >= 1),
    frames_rendered         bigint NOT NULL CHECK (frames_rendered >= 1),
    first_changing_frame    bigint CHECK (first_changing_frame IS NULL OR first_changing_frame >= 1),
    last_changing_frame     bigint CHECK (last_changing_frame IS NULL OR last_changing_frame >= first_changing_frame),
    final_frame_sha256      text NOT NULL CHECK (final_frame_sha256 ~ '^[0-9a-f]{64}$'),
    frame_set_digest        text NOT NULL CHECK (frame_set_digest ~ '^[0-9a-f]{64}$'),
    concurrency             integer NOT NULL CHECK (concurrency >= 1),
    arguments               text NOT NULL,
    request_sha256          text NOT NULL CHECK (request_sha256 ~ '^[0-9a-f]{64}$'),
    render_seconds          numeric NOT NULL CHECK (render_seconds >= 0),
    leftover_processes      integer NOT NULL,
    sameness                text NOT NULL CHECK (sameness IN ('RepeatsTwoRenders', 'DifferedTwoRenders', 'EqualsApprovedStill', 'RetimedRenderedOnce')),
    sameness_statement      text NOT NULL CHECK (
                                sameness_statement LIKE 'repeats%' OR sameness_statement LIKE 'not observed to repeat%'
                                OR sameness_statement LIKE 'equals the owner''s approved still%' OR sameness_statement LIKE 're-timed%'),
    recorded_at             timestamptz NOT NULL,
    PRIMARY KEY (item_id, item_version, relative_path),
    FOREIGN KEY (item_id, item_version, relative_path) REFERENCES production_artifacts (item_id, item_version, relative_path),
    -- A still is one frame; a motion graphic renders exactly its segment's frames.
    CHECK ((kind = 'Still' AND frames_rendered = 1) OR (kind = 'Motion' AND frames_rendered = segment_frames)),
    -- A sameness statement made only where its comparison could be made.
    CHECK (sameness NOT IN ('EqualsApprovedStill') OR kind = 'Still'),
    CHECK (sameness NOT IN ('RetimedRenderedOnce') OR kind = 'Motion')
);

CREATE OR REPLACE TRIGGER graphic_provenance_stamped
    BEFORE INSERT ON graphic_provenance
    FOR EACH ROW EXECUTE FUNCTION stamp_production_row();

CREATE OR REPLACE TRIGGER graphic_provenance_write_once
    BEFORE UPDATE OR DELETE ON graphic_provenance
    FOR EACH ROW EXECUTE FUNCTION refuse_production_amendment();

COMMIT;
