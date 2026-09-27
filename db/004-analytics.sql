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
--
-- THE MEASUREMENT CASE IS ENCODED IN THE ROW, not inferred from whether a number happens to be
-- there. The three cases of a measurement quantity are three distinguishable row shapes, and the
-- table check below admits exactly those three and nothing else. The first shape that was written
-- here stored an observed zero as a null amount, which the reader then returned as never
-- measured -- the collapse this whole change exists to prevent, running in the direction from the
-- store back to the surface. Encoding the case rather than deducing it from a null is what makes
-- that shape unwritable rather than merely unwritten.

BEGIN;

CREATE TABLE observed_revenue_parameters (
    parameter_id       uuid PRIMARY KEY,
    recorded_at        timestamptz NOT NULL,

    -- The observed cases. An observed value carries a non-zero amount and its unit; an OBSERVED
    -- ZERO carries an amount of exactly zero and its unit, and is a different fact from a row
    -- that records no amount at all.
    amount             numeric(18, 8),
    unit               text,

    -- The unmeasured case. Both are present together or neither is, and they are present only
    -- where no amount is: an unmeasured quantity carries no value field of any kind, and the
    -- reason set is closed at the same two members the surface declares.
    unmeasured_reason  text CHECK (
        unmeasured_reason IS NULL
        OR unmeasured_reason IN ('NoObservationExists', 'SourceCannotStateOne')
    ),
    unmeasured_detail  text,

    -- The observation the parameter came from, and its date. Both present or both absent: a source
    -- with no date cannot be re-checked, and a date with no source names nothing.
    source_observation text,
    observed_on        date,

    -- Exactly one of the two shapes, so a half-filled row cannot exist and no reader has to guess
    -- which case a row is in.
    CONSTRAINT observed_revenue_parameters_measurement_case CHECK (
        (amount IS NOT NULL AND unit IS NOT NULL AND unit <> ''
             AND unmeasured_reason IS NULL AND unmeasured_detail IS NULL)
        OR
        (amount IS NULL AND unit IS NULL
             AND unmeasured_reason IS NOT NULL AND unmeasured_detail IS NOT NULL AND unmeasured_detail <> '')
    ),

    CONSTRAINT observed_revenue_parameters_source_is_dated CHECK (
        (source_observation IS NULL     AND observed_on IS NULL) OR
        (source_observation IS NOT NULL AND observed_on IS NOT NULL)
    )
);

-- The admitting rows, as the datastore sees them. A row appears here only where it carries a
-- non-zero amount, a source observation and that observation's date; every other row is recorded
-- and admits nothing. The view is what makes "no parameter is observed" a query rather than a
-- claim, and it excludes an observed zero deliberately: a revenue parameter observed to be zero
-- is a real observation and still lights no figure derived by dividing by it.
CREATE VIEW v_observed_revenue_parameters AS
SELECT parameter_id, recorded_at, amount, unit, source_observation, observed_on
FROM observed_revenue_parameters
WHERE amount IS NOT NULL
  AND amount <> 0
  AND source_observation IS NOT NULL
  AND observed_on IS NOT NULL;

COMMIT;
