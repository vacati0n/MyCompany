-- Wave 4, the analytics surface. Additive: one register, and nothing else.
--
-- No delivered table, column, constraint or view is altered or dropped, so no data migration over
-- existing rows arises. The file is applied after every resource it depends on, exactly as the
-- previous wave's file was.
--
-- The register created here records an OBSERVED revenue parameter: one carrying the source
-- observation it came from and the date of that observation. A row recorded without both is an
-- ASSUMED parameter, and the admission rule in the analytics surface refuses it, so it lights
-- none of the six revenue-derived figures.
--
-- The register is created EMPTY and this change records no row in it. While it holds no admitting
-- row the six figures are visible in no view, which is their default state rather than a filtered
-- one. Reversal is therefore lossless here: dropping the register loses no recorded observation.

BEGIN;

CREATE TABLE observed_revenue_parameters (
    parameter_id       uuid PRIMARY KEY,
    recorded_at        timestamptz NOT NULL,

    -- The parameter itself. Nullable because a row MAY be recorded without an amount, which is a
    -- different fact from an amount of zero and is reported as unmeasured rather than as zero.
    amount             numeric(18, 8),
    unit               text NOT NULL,

    -- The observation the parameter came from, and its date. Both present or both absent: a source
    -- with no date cannot be re-checked, and a date with no source names nothing.
    source_observation text,
    observed_on        date,

    CONSTRAINT observed_revenue_parameters_source_is_dated CHECK (
        (source_observation IS NULL     AND observed_on IS NULL) OR
        (source_observation IS NOT NULL AND observed_on IS NOT NULL)
    ),

    CONSTRAINT observed_revenue_parameters_unit_is_stated CHECK (unit <> '')
);

-- The admitting rows, as the datastore sees them. A row appears here only where it carries an
-- amount, a source observation and that observation's date; every other row is recorded and
-- admits nothing. The view is what makes "no parameter is observed" a query rather than a claim.
CREATE VIEW v_observed_revenue_parameters AS
SELECT parameter_id, recorded_at, amount, unit, source_observation, observed_on
FROM observed_revenue_parameters
WHERE amount IS NOT NULL
  AND amount <> 0
  AND source_observation IS NOT NULL
  AND observed_on IS NOT NULL;

COMMIT;
