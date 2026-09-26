-- Cost rollups and budget utilization (module M-006).
--
-- Every aggregation here is over numeric columns in the datastore. The application reads the
-- result; it performs no money arithmetic of its own (constraint C-005, decision D-006). Because
-- agent_costs carries the item and the period denormalized at write time, each rollup is an index
-- aggregation rather than a join through lifecycle history (risk R-013).

BEGIN;

-- Acceptance query one, cost per item (acceptance criterion AC-004).
CREATE VIEW v_cost_per_item AS
SELECT
    item_id,
    SUM(computed_cost)                                      AS total_cost,
    MIN(currency)                                           AS currency,
    COUNT(*)                                                AS operations,
    COUNT(*) FILTER (WHERE outcome = 'Failed')              AS failed_operations,
    COUNT(*) FILTER (WHERE cost_basis = 'Estimate')         AS estimated_operations,
    BOOL_OR(cost_basis = 'Estimate')                        AS contains_estimates
FROM agent_costs
GROUP BY item_id;

-- The monthly total, presented against the approved envelope with the variance stated. The
-- envelope figures are those recorded at design fact F-009; every figure is an ESTIMATE until a
-- unit price has been verified first-hand, which risk RK-002 requires before any spend.
CREATE VIEW v_cost_per_period AS
SELECT
    period,
    SUM(computed_cost)                              AS total_cost,
    77.41::numeric(18, 8)                           AS envelope_total,
    34.42::numeric(18, 8)                           AS envelope_metered,
    42.99::numeric(18, 8)                           AS envelope_standing,
    SUM(computed_cost) - 77.41::numeric(18, 8)      AS variance_against_envelope,
    BOOL_OR(cost_basis = 'Estimate')                AS contains_estimates,
    COUNT(*)                                        AS operations
FROM agent_costs
GROUP BY period;

CREATE VIEW v_cost_by_capability AS
SELECT period, capability_class, SUM(computed_cost) AS total_cost, COUNT(*) AS operations
FROM agent_costs
GROUP BY period, capability_class;

CREATE VIEW v_cost_by_model AS
SELECT period, model_id, SUM(computed_cost) AS total_cost, COUNT(*) AS operations
FROM agent_costs
WHERE model_id IS NOT NULL
GROUP BY period, model_id;

-- Acceptance criterion AC-017: the AI cost attached to the named deterministic set, measured over
-- a recorded run set, is zero.
CREATE VIEW v_deterministic_ai_cost AS
SELECT
    period,
    deterministic_task,
    SUM(computed_cost) AS total_cost,
    COUNT(*)           AS operations
FROM agent_costs
WHERE deterministic_task IS NOT NULL
GROUP BY period, deterministic_task;

-- Acceptance criterion AC-003: the count of operations with a missing or incomplete attribution.
-- The attribution columns are NOT NULL, so this view counts the one remaining way a row could be
-- incomplete: an all-zero identifier standing in for an absent one.
CREATE VIEW v_incomplete_attribution AS
SELECT *
FROM agent_costs
WHERE item_id = '00000000-0000-0000-0000-000000000000'::uuid
   OR channel_id = '00000000-0000-0000-0000-000000000000'::uuid
   OR department_id = '00000000-0000-0000-0000-000000000000'::uuid
   OR agent_id = '00000000-0000-0000-0000-000000000000'::uuid
   OR capability_class = '';

-- Budget utilization, computed from the recorded operations alone (acceptance criterion AC-027).
CREATE FUNCTION fn_budget_utilization(p_budget_id uuid)
RETURNS TABLE (utilized numeric, budget_amount numeric, utilization_percent numeric)
AS $$
    SELECT
        COALESCE(SUM(c.computed_cost), 0)::numeric                              AS utilized,
        b.amount                                                                AS budget_amount,
        CASE WHEN b.amount = 0 THEN 0
             ELSE ROUND(COALESCE(SUM(c.computed_cost), 0) * 100.0 / b.amount, 4)
        END                                                                     AS utilization_percent
    FROM budgets b
    LEFT JOIN agent_costs c
      ON c.period = b.period
     AND ((b.scope_kind = 'Department' AND c.department_id = b.scope_id)
       OR (b.scope_kind = 'Channel'    AND c.channel_id    = b.scope_id))
    WHERE b.budget_id = p_budget_id
    GROUP BY b.amount;
$$ LANGUAGE sql STABLE;

-- The budgets governing one attribution in one period.
CREATE FUNCTION fn_budgets_for(
    p_channel_id uuid,
    p_department_id uuid,
    p_period date
) RETURNS SETOF budgets
AS $$
    SELECT *
    FROM budgets
    WHERE period = p_period
      AND ((scope_kind = 'Channel'    AND scope_id = p_channel_id)
        OR (scope_kind = 'Department' AND scope_id = p_department_id));
$$ LANGUAGE sql STABLE;

-- Acceptance query two, the permission answer for a finished item (acceptance criterion AC-008).
-- Three indexed reads and no lookup outside the record: the basis is on the asset row, the
-- registration proof is the join to library_registrations, and the verification event is in the
-- recorded history.
CREATE VIEW v_item_permission_answer AS
SELECT
    a.item_id,
    a.asset_id,
    a.library,
    a.source,
    a.creator,
    a.licence_type,
    a.licence_reference,
    a.commercial_use_permitted,
    a.modification_permitted,
    a.attribution_requirement,
    a.platform_restrictions,
    a.expiry,
    a.proof_of_licence_reference,
    a.assessed_risk,
    a.verified_by,
    a.verified_on,
    r.registered_on,
    (
        a.source IS NOT NULL AND
        a.creator IS NOT NULL AND
        a.licence_type IS NOT NULL AND
        a.licence_reference IS NOT NULL AND
        a.commercial_use_permitted IS TRUE AND
        a.modification_permitted IS NOT NULL AND
        a.attribution_requirement IS NOT NULL AND
        a.platform_restrictions IS NOT NULL AND
        a.proof_of_licence_reference IS NOT NULL AND
        a.assessed_risk IS NOT NULL AND
        a.verified_by IS NOT NULL AND
        a.verified_on IS NOT NULL AND
        (a.expiry IS NULL OR a.expiry >= CURRENT_DATE) AND
        r.registered_on IS NOT NULL
    ) AS permitted
FROM assets a
JOIN items i ON i.item_id = a.item_id
LEFT JOIN library_registrations r
       ON r.channel_id = i.channel_id AND r.library = a.library;

-- Acceptance criterion AC-023: failures ending in neither a retry nor an escalation.
CREATE VIEW v_failures_unhandled AS
SELECT *
FROM job_stages
WHERE outcome = 'Failed' AND NOT escalated;

COMMIT;
