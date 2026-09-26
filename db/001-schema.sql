-- Media company foundation, wave 1 — initial schema.
--
-- Stack O-005, selected by the owner at the Design Gate: PostgreSQL is the single datastore
-- holding relational state, the append-only record, and the durable job queue as a transactional
-- claim table. Two properties of the technical design live here rather than in application code:
--
--   * Exactly-once accounting (constraint C-004). The state change, the queue entry, the
--     operation record and the audit entry commit in one transaction, so there is no window
--     between an operation and its record and no outbox is needed.
--
--   * Exact, re-derivable money arithmetic (constraint C-005, decision D-006). Every cost is a
--     numeric GENERATED column over the unit counts and the applied unit prices stored on the
--     same row, and every rollup is an aggregation over those columns. No money arithmetic is
--     performed in application code, and a cost stays re-derivable after the price row that
--     produced it has been superseded.
--
-- Initial creation only; there is nothing to migrate (constraint C-012).

BEGIN;

-- ---------------------------------------------------------------------------
-- Registers (module M-011)
-- ---------------------------------------------------------------------------

CREATE TABLE companies (
    company_id      uuid PRIMARY KEY,
    name            text NOT NULL,
    operating_state text NOT NULL
);

CREATE TABLE channels (
    channel_id  uuid PRIMARY KEY,
    company_id  uuid NOT NULL REFERENCES companies (company_id),
    platform    text NOT NULL,
    language    text NOT NULL,
    registered  boolean NOT NULL DEFAULT false
);

CREATE TABLE departments (
    department_id uuid PRIMARY KEY,
    company_id    uuid NOT NULL REFERENCES companies (company_id),
    name          text NOT NULL,
    budget_holder text NOT NULL
);

CREATE TABLE workforce_agents (
    agent_id      uuid PRIMARY KEY,
    department_id uuid NOT NULL REFERENCES departments (department_id),
    name          text NOT NULL,
    role          text NOT NULL
);

CREATE TABLE items (
    item_id      uuid PRIMARY KEY,
    channel_id   uuid NOT NULL REFERENCES channels (channel_id),
    item_version integer NOT NULL DEFAULT 1,
    title        text NOT NULL
);

CREATE TABLE provider_accounts (
    provider_account_id     text PRIMARY KEY,
    provider                text NOT NULL,
    commercial_terms_basis  text NOT NULL,
    verified_on             date NOT NULL,
    status                  text NOT NULL
        CONSTRAINT provider_accounts_status_closed
        CHECK (status IN ('Active', 'Disabled', 'Revoked')),
    -- Constraint C-002: one production account per provider.
    CONSTRAINT provider_accounts_one_per_provider UNIQUE (provider)
);

CREATE TABLE models (
    model_id            text PRIMARY KEY,
    provider_account_id text NOT NULL REFERENCES provider_accounts (provider_account_id),
    rated_quality       integer NOT NULL CHECK (rated_quality BETWEEN 0 AND 100),
    context_capacity    integer NOT NULL CHECK (context_capacity >= 0),
    modality            text NOT NULL
);

-- Temporal price rows, never updated in place (decision D-010). A superseded row keeps its
-- validity interval, which is what keeps a cost re-derivable years later (risk R-006).
CREATE TABLE model_prices (
    model_price_id uuid PRIMARY KEY,
    model_id       text NOT NULL REFERENCES models (model_id),
    unit_kind      text NOT NULL
        CHECK (unit_kind IN ('InputUnit', 'OutputUnit', 'CachedUnit', 'PerOperation', 'PerSecond')),
    unit_price     numeric(18, 10) NOT NULL CHECK (unit_price >= 0),
    currency       text NOT NULL,
    source         text NOT NULL,
    verified_on    date NOT NULL,
    valid_from     timestamptz NOT NULL,
    valid_to       timestamptz,
    CONSTRAINT model_prices_interval_ordered CHECK (valid_to IS NULL OR valid_to > valid_from)
);

CREATE INDEX model_prices_in_force
    ON model_prices (model_id, unit_kind, valid_from DESC);

-- ---------------------------------------------------------------------------
-- Routing position and the forbidden-source register (modules M-002, M-004)
-- ---------------------------------------------------------------------------

CREATE TABLE forbidden_sources (
    kind               text NOT NULL
        CHECK (kind IN ('ConsumerChatSubscription', 'MultipliedPersonalAccount', 'SharedCredential',
                        'AutomatedAccessProhibited', 'WithdrawnInterface')),
    identifier         text NOT NULL,
    reason             text NOT NULL,
    evidence_reference text NOT NULL,
    PRIMARY KEY (kind, identifier)
);

CREATE TABLE routes (
    route_id          uuid PRIMARY KEY,
    capability_class  text NOT NULL,
    tier              text NOT NULL CHECK (tier IN ('Primary', 'Secondary', 'Emergency')),
    target_kind       text NOT NULL CHECK (target_kind IN ('ProviderRoute', 'HoldAndEscalate', 'NonAiSubstitute')),
    provider_account_id text REFERENCES provider_accounts (provider_account_id),
    model_id          text REFERENCES models (model_id),
    substitute_task   text,
    hold_reason       text,
    rated_quality     integer NOT NULL CHECK (rated_quality BETWEEN 0 AND 100),
    context_capacity  integer NOT NULL CHECK (context_capacity >= 0),
    terms_basis       text NOT NULL,
    terms_verified_on date NOT NULL,

    -- Constraint C-002: exactly one primary, one secondary and one emergency per capability.
    CONSTRAINT routes_one_per_tier UNIQUE (capability_class, tier),

    -- The typed emergency union of decision D-009, enforced so that a half-filled row cannot exist.
    CONSTRAINT routes_target_union CHECK (
        (target_kind = 'ProviderRoute'   AND provider_account_id IS NOT NULL AND model_id IS NOT NULL
                                         AND substitute_task IS NULL AND hold_reason IS NULL) OR
        (target_kind = 'HoldAndEscalate' AND hold_reason IS NOT NULL
                                         AND provider_account_id IS NULL AND model_id IS NULL AND substitute_task IS NULL) OR
        (target_kind = 'NonAiSubstitute' AND substitute_task IS NOT NULL
                                         AND provider_account_id IS NULL AND model_id IS NULL AND hold_reason IS NULL)
    )
);

-- Route admission is refused in the database as well as in the application, which is what makes
-- the two refusals of constraint C-001 independent: a configuration error that got past the
-- application check is still refused here.
CREATE FUNCTION refuse_forbidden_route() RETURNS trigger AS $$
DECLARE
    hit forbidden_sources%ROWTYPE;
BEGIN
    SELECT * INTO hit
    FROM forbidden_sources f
    WHERE f.identifier IN (
        COALESCE(NEW.provider_account_id, ''),
        COALESCE(NEW.model_id, ''),
        COALESCE(NEW.substitute_task, '')
    )
    LIMIT 1;

    IF FOUND THEN
        RAISE EXCEPTION
            'route % refused: % is a forbidden source (%)', NEW.route_id, hit.identifier, hit.reason
            USING ERRCODE = 'integrity_constraint_violation';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER routes_refuse_forbidden
    BEFORE INSERT OR UPDATE ON routes
    FOR EACH ROW EXECUTE FUNCTION refuse_forbidden_route();

-- Availability is an explicit recorded state with recorded transitions (decision D-008).
CREATE TABLE route_availability (
    route_id            uuid NOT NULL REFERENCES routes (route_id),
    effective_from      timestamptz NOT NULL,
    state               text NOT NULL
        CHECK (state IN ('Serving', 'QuotaExhausted', 'Outage', 'QualityDegraded', 'AccountDisabled', 'AccountRevoked')),
    reason              text NOT NULL,
    reset_or_probe_point timestamptz,
    observed_quality    integer CHECK (observed_quality IS NULL OR observed_quality BETWEEN 0 AND 100),
    PRIMARY KEY (route_id, effective_from)
);

-- Current state in one seek (design: route and effective-from descending).
CREATE INDEX route_availability_current
    ON route_availability (route_id, effective_from DESC);

-- ---------------------------------------------------------------------------
-- Temporal configuration (module M-012)
-- ---------------------------------------------------------------------------

CREATE TABLE configuration (
    config_key text NOT NULL,
    scope      text NOT NULL,
    valid_from timestamptz NOT NULL,
    valid_to   timestamptz,
    value      text NOT NULL,
    version    integer NOT NULL,
    changed_by text NOT NULL,
    reason     text NOT NULL,
    PRIMARY KEY (config_key, scope, valid_from),
    CONSTRAINT configuration_interval_ordered CHECK (valid_to IS NULL OR valid_to > valid_from)
);

CREATE INDEX configuration_in_force ON configuration (config_key, scope, valid_from DESC);

-- ---------------------------------------------------------------------------
-- Work lifecycle and the durable job queue as a claim table (module M-013)
-- ---------------------------------------------------------------------------

CREATE TABLE jobs (
    job_id           uuid PRIMARY KEY,
    item_id          uuid NOT NULL REFERENCES items (item_id),
    channel_id       uuid NOT NULL REFERENCES channels (channel_id),
    workflow         text NOT NULL,
    position         text NOT NULL,
    claim_state      text NOT NULL CHECK (claim_state IN ('Ready', 'Claimed', 'Done', 'Dead')),
    available_at     timestamptz NOT NULL,
    claimed_by       text,
    lease_expires_at timestamptz
);

-- The claim index. A worker takes the oldest ready job with FOR UPDATE SKIP LOCKED, so two
-- workers never take the same job and independent work progresses independently (AC-024).
CREATE INDEX jobs_ready ON jobs (available_at) WHERE claim_state = 'Ready';

CREATE TABLE job_stages (
    job_id         uuid NOT NULL REFERENCES jobs (job_id),
    position       text NOT NULL,
    entered_at     timestamptz NOT NULL,
    outcome        text NOT NULL
        CHECK (outcome IN ('Pending', 'Succeeded', 'Retried', 'Escalated', 'Failed', 'Held')),
    attempts       integer NOT NULL CHECK (attempts >= 0),
    escalated      boolean NOT NULL,
    left_at        timestamptz,
    failure_reason text,
    PRIMARY KEY (job_id, position, entered_at)
);

CREATE TABLE agent_runs (
    run_id     uuid PRIMARY KEY,
    job_id     uuid NOT NULL REFERENCES jobs (job_id),
    position   text NOT NULL,
    agent_id   uuid NOT NULL REFERENCES workforce_agents (agent_id),
    started_at timestamptz NOT NULL,
    ended_at   timestamptz,
    outcome    text
);

-- ---------------------------------------------------------------------------
-- Per-operation accounting (module M-005), one row per attempt (decision D-011)
-- ---------------------------------------------------------------------------

CREATE TABLE agent_costs (
    operation_id      uuid PRIMARY KEY,
    run_id            uuid NOT NULL,
    occurred_at       timestamptz NOT NULL,
    attempt           integer NOT NULL CHECK (attempt >= 1),

    -- The attribution tuple, required on every row (constraint C-004). Item and period are
    -- denormalized at write time so the rollup is an index aggregation rather than a join through
    -- lifecycle history years later (risk R-013).
    item_id           uuid NOT NULL,
    channel_id        uuid NOT NULL,
    department_id     uuid NOT NULL,
    agent_id          uuid NOT NULL,
    capability_class  text NOT NULL,
    period            date NOT NULL,

    route_id          uuid REFERENCES routes (route_id),
    model_id          text REFERENCES models (model_id),

    -- Set when the operation served a member of the named zero-AI-cost set. Acceptance criterion
    -- AC-017 measures the cost attached to these rows to zero.
    deterministic_task text,

    input_units       bigint NOT NULL DEFAULT 0 CHECK (input_units >= 0),
    output_units      bigint NOT NULL DEFAULT 0 CHECK (output_units >= 0),
    cached_units      bigint NOT NULL DEFAULT 0 CHECK (cached_units >= 0),
    other_units       bigint NOT NULL DEFAULT 0 CHECK (other_units >= 0),

    -- The applied price row, retained so the arithmetic stays re-derivable after the price has
    -- been superseded, together with the unit prices that row carried at the moment of the write.
    applied_price_id      uuid REFERENCES model_prices (model_price_id),
    applied_input_price   numeric(18, 10) NOT NULL DEFAULT 0 CHECK (applied_input_price >= 0),
    applied_output_price  numeric(18, 10) NOT NULL DEFAULT 0 CHECK (applied_output_price >= 0),
    applied_cached_price  numeric(18, 10) NOT NULL DEFAULT 0 CHECK (applied_cached_price >= 0),
    currency              text NOT NULL DEFAULT 'USD',

    -- The money arithmetic. It is here, in the datastore's exact decimal type, and nowhere in
    -- application code (constraint C-005, decision D-006).
    computed_cost numeric(18, 8) GENERATED ALWAYS AS (
        (input_units  * applied_input_price) +
        (output_units * applied_output_price) +
        (cached_units * applied_cached_price)
    ) STORED,

    -- Whether the units were measured from the provider's response or estimated (risk R-009).
    cost_basis      text NOT NULL CHECK (cost_basis IN ('Measurement', 'Estimate')),
    duration_ms     bigint NOT NULL CHECK (duration_ms >= 0),
    outcome         text NOT NULL CHECK (outcome IN ('Succeeded', 'Failed', 'Refused', 'Held')),
    failure_reason  text,

    -- A member of the named deterministic set must carry no price and no units, so a non-zero
    -- cost cannot be attached to it even by a faulty writer.
    CONSTRAINT agent_costs_deterministic_is_free CHECK (
        deterministic_task IS NULL OR (
            applied_price_id IS NULL AND
            applied_input_price = 0 AND applied_output_price = 0 AND applied_cached_price = 0
        )
    )
);

-- Indexes named against the queries the design's acceptance queries need.
CREATE INDEX agent_costs_by_item    ON agent_costs (item_id, occurred_at) INCLUDE (computed_cost, input_units, output_units, cached_units);
CREATE INDEX agent_costs_by_channel ON agent_costs (channel_id, period)   INCLUDE (computed_cost);
CREATE INDEX agent_costs_by_dept    ON agent_costs (department_id, period) INCLUDE (computed_cost);
CREATE INDEX agent_costs_by_cap     ON agent_costs (capability_class, period) INCLUDE (computed_cost);
CREATE INDEX agent_costs_by_model   ON agent_costs (model_id, period) INCLUDE (computed_cost);
CREATE INDEX agent_costs_determ     ON agent_costs (period) WHERE deterministic_task IS NOT NULL;

-- ---------------------------------------------------------------------------
-- Budgets and threshold alerts (module M-006)
-- ---------------------------------------------------------------------------

CREATE TABLE budgets (
    budget_id  uuid PRIMARY KEY,
    scope_kind text NOT NULL CHECK (scope_kind IN ('Department', 'Channel')),
    scope_id   uuid NOT NULL,
    period     date NOT NULL,
    amount     numeric(18, 8) NOT NULL CHECK (amount > 0),
    currency   text NOT NULL DEFAULT 'USD',
    -- Each department and each channel resolves to exactly one budget per period (AC-027).
    CONSTRAINT budgets_one_per_scope_period UNIQUE (scope_kind, scope_id, period)
);

CREATE TABLE budget_alerts (
    budget_id   uuid NOT NULL REFERENCES budgets (budget_id),
    period      date NOT NULL,
    threshold   integer NOT NULL CHECK (threshold IN (50, 75, 90, 100)),
    utilization numeric(9, 4) NOT NULL,
    utilized    numeric(18, 8) NOT NULL,
    budget_amount numeric(18, 8) NOT NULL,
    raised_at   timestamptz NOT NULL,
    -- Idempotent per budget, period and threshold (constraint C-018).
    PRIMARY KEY (budget_id, period, threshold)
);

-- ---------------------------------------------------------------------------
-- Asset and rights ledger (module M-015)
-- ---------------------------------------------------------------------------

CREATE TABLE library_registrations (
    channel_id    uuid NOT NULL REFERENCES channels (channel_id),
    library       text NOT NULL,
    registered_on date NOT NULL,
    PRIMARY KEY (channel_id, library)
);

-- Every permission-basis column is nullable on purpose: an incomplete record must be
-- representable so that it can resolve as not permitted (constraint C-022), rather than being
-- rejected at write time and never appearing in the record at all.
CREATE TABLE assets (
    asset_id                 uuid PRIMARY KEY,
    item_id                  uuid NOT NULL REFERENCES items (item_id),
    library                  text NOT NULL,
    source                   text,
    creator                  text,
    licence_type             text,
    licence_reference        text,
    commercial_use_permitted boolean,
    modification_permitted   boolean,
    attribution_requirement  text,
    platform_restrictions    text,
    expiry                   date,
    proof_of_licence_reference text,
    assessed_risk            text,
    verified_by              text,
    verified_on              date
);

CREATE INDEX assets_by_item ON assets (item_id);

-- ---------------------------------------------------------------------------
-- Authority, controls and the publication gate (modules M-008, M-009)
-- ---------------------------------------------------------------------------

-- The closed action set lives in code (module M-008, decision D-003 layer one). These tables
-- mirror it so the operating registers can be produced from the company's own records; a test
-- asserts the mirror equals the closed set, so a drift is a failing check rather than a silent
-- widening.
CREATE TABLE roles (
    role text PRIMARY KEY
);

CREATE TABLE role_permissions (
    role   text NOT NULL REFERENCES roles (role),
    action text NOT NULL,
    PRIMARY KEY (role, action)
);

CREATE TABLE controls (
    control_id   text PRIMARY KEY,
    description  text NOT NULL,
    non_cuttable boolean NOT NULL
);

-- The subject-role relation is required: a control with no subject role cannot be admitted, which
-- is the mitigation recorded against risk R-008.
CREATE TABLE control_subject_roles (
    control_id text NOT NULL REFERENCES controls (control_id) ON DELETE CASCADE,
    role       text NOT NULL REFERENCES roles (role),
    PRIMARY KEY (control_id, role)
);

CREATE TABLE control_granted_actions (
    control_id text NOT NULL REFERENCES controls (control_id) ON DELETE CASCADE,
    action     text NOT NULL,
    PRIMARY KEY (control_id, action)
);

CREATE TABLE exception_requests (
    exception_id text PRIMARY KEY,
    control_id   text NOT NULL REFERENCES controls (control_id),
    requester    text NOT NULL REFERENCES roles (role),
    reason       text NOT NULL,
    requested_at timestamptz NOT NULL
);

CREATE TABLE exception_approvals (
    exception_id text PRIMARY KEY REFERENCES exception_requests (exception_id),
    approver     text NOT NULL REFERENCES roles (role),
    granted      boolean NOT NULL,
    reason       text NOT NULL,
    decided_at   timestamptz NOT NULL
);

-- No role approves an exception it requested, and no role approves an exception to a control it
-- is subject to. Both refusals are enforced here as well as in the application, so neither rests
-- on one code path (acceptance criterion AC-012).
CREATE FUNCTION refuse_self_approval() RETURNS trigger AS $$
DECLARE
    requester_role text;
    subject_hit    boolean;
BEGIN
    SELECT r.requester INTO requester_role
    FROM exception_requests r WHERE r.exception_id = NEW.exception_id;

    IF requester_role = NEW.approver THEN
        RAISE EXCEPTION 'exception % refused: % requested it and may not approve it',
            NEW.exception_id, NEW.approver
            USING ERRCODE = 'integrity_constraint_violation';
    END IF;

    SELECT EXISTS (
        SELECT 1
        FROM exception_requests r
        JOIN control_subject_roles s ON s.control_id = r.control_id
        WHERE r.exception_id = NEW.exception_id AND s.role = NEW.approver
    ) INTO subject_hit;

    IF subject_hit THEN
        RAISE EXCEPTION 'exception % refused: % is a subject role of the control',
            NEW.exception_id, NEW.approver
            USING ERRCODE = 'integrity_constraint_violation';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER exception_approvals_refuse_self
    BEFORE INSERT OR UPDATE ON exception_approvals
    FOR EACH ROW EXECUTE FUNCTION refuse_self_approval();

CREATE TABLE blocks (
    block_id   uuid PRIMARY KEY,
    item_id    uuid NOT NULL REFERENCES items (item_id),
    asset_id   uuid REFERENCES assets (asset_id),
    placed_by  text NOT NULL REFERENCES roles (role),
    reason     text NOT NULL,
    open       boolean NOT NULL DEFAULT true,
    placed_at  timestamptz NOT NULL
);

CREATE INDEX blocks_open ON blocks (item_id) WHERE open;

-- An approval binds to an exact item version (constraint C-016): the uniqueness is over the
-- version, so an approval cannot be reused for a changed one.
CREATE TABLE approvals (
    item_id         uuid NOT NULL REFERENCES items (item_id),
    item_version    integer NOT NULL,
    gate            text NOT NULL,
    approver        text NOT NULL REFERENCES roles (role),
    verdict         text NOT NULL CHECK (verdict IN ('Approved', 'SentBack')),
    reason          text NOT NULL,
    presented_at    timestamptz NOT NULL,
    decided_at      timestamptz NOT NULL,

    -- Derived from the two recorded timestamps, never entered (decision D-007, risk RK-005).
    elapsed_minutes integer GENERATED ALWAYS AS (
        GREATEST(0, (EXTRACT(EPOCH FROM (decided_at - presented_at)) / 60)::integer)
    ) STORED,

    PRIMARY KEY (item_id, item_version, gate, approver, presented_at),
    CONSTRAINT approvals_decided_after_presented CHECK (decided_at >= presented_at)
);

CREATE INDEX approvals_by_version ON approvals (item_id, item_version, gate);

CREATE TABLE gate_transitions (
    item_id      uuid NOT NULL REFERENCES items (item_id),
    item_version integer NOT NULL,
    occurred_at  timestamptz NOT NULL,
    from_state   text NOT NULL,
    to_state     text NOT NULL,
    reason       text NOT NULL,
    PRIMARY KEY (item_id, item_version, occurred_at)
);

-- The transition table of decision D-007 lives in code. This trigger refuses the one transition
-- that would matter if the code were wrong: reaching Published from anything but Approved.
CREATE FUNCTION refuse_gate_bypass() RETURNS trigger AS $$
BEGIN
    IF NEW.to_state = 'Published' AND NEW.from_state <> 'Approved' THEN
        RAISE EXCEPTION 'gate bypass refused: Published is reachable only from Approved, not from %',
            NEW.from_state
            USING ERRCODE = 'integrity_constraint_violation';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER gate_transitions_refuse_bypass
    BEFORE INSERT ON gate_transitions
    FOR EACH ROW EXECUTE FUNCTION refuse_gate_bypass();

-- ---------------------------------------------------------------------------
-- The append-only record (module M-007)
-- ---------------------------------------------------------------------------

CREATE TABLE audit_entries (
    entry_id            uuid PRIMARY KEY,
    sequence_no         bigserial NOT NULL UNIQUE,
    actor               text NOT NULL,
    action              text NOT NULL,
    subject             text NOT NULL,
    occurred_at         timestamptz NOT NULL,
    reason              text NOT NULL,
    inputs_reference    text NOT NULL,
    outputs_reference   text NOT NULL,
    decision            text NOT NULL,
    cost_reference      text NOT NULL,
    risk                text NOT NULL,
    retention_class     text NOT NULL
        CHECK (retention_class IN ('OperationalRecord', 'FinancialRecord', 'RightsRecord', 'GovernanceRecord')),
    previous_entry_hash text NOT NULL,
    entry_hash          text NOT NULL UNIQUE
);

CREATE INDEX audit_entries_by_subject ON audit_entries (subject, occurred_at);

-- Constraint C-014. An amendment or a deletion is refused at the datastore, so the record is not
-- correctable in place even by a caller holding the connection. The refusal is recorded as its
-- own entry by the application, which catches this exception and appends it — that write is a
-- separate transaction precisely because this one is rolled back.
CREATE FUNCTION refuse_audit_amendment() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'the recorded history is append-only: % on entry % is refused',
        TG_OP, COALESCE(OLD.entry_id::text, '(unknown)')
        USING ERRCODE = 'integrity_constraint_violation';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER audit_entries_append_only
    BEFORE UPDATE OR DELETE ON audit_entries
    FOR EACH ROW EXECUTE FUNCTION refuse_audit_amendment();

COMMIT;
