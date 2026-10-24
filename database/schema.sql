-- =====================================================================
-- Vigía — Esquema de base de datos (PostgreSQL)
-- Trazza Labs · UPC 1ASI0730 Aplicaciones Web · sección 8084
--
-- Derivado de la sección 4.8.1 "Database Diagrams" del informe.
-- Monolito modular: los 8 Bounded Contexts de la sección 4.6.1 viven en
-- el mismo esquema `public` y se mantienen separados por diseño, no por
-- barrera física. No existen claves foráneas entre contextos (4.8).
--
-- Convenciones aplicadas:
--   · Tablas            → plural, snake_case        (demo_requests)
--   · Columnas          → singular, snake_case      (contact_full_name)
--   · Primary key       → pk_<tabla>
--   · Foreign key       → fk_<tabla>_<tabla_referenciada>
--   · Unique            → uq_<tabla>_<columna>
--   · Check             → ck_<tabla>_<concepto>
--   · Índice            → idx_<tabla>_<columnas>
--   · Enumeraciones     → varchar + CHECK (no tablas de catálogo, por 4.8)
--   · Marcas de tiempo  → timestamptz (mapea a DateTimeOffset en C#)
--
-- La convención tabla-plural / columna-snake_case coincide con la que ya
-- aplica UseSnakeCaseNamingConvention() en ModelBuilderExtensions.cs, de
-- modo que las migraciones de EF Core produzcan estos mismos nombres.
--
-- Requiere PostgreSQL 13 o superior (gen_random_uuid() en el core).
--
-- Creación previa de la base de datos (fuera de este script, porque
-- CREATE DATABASE no admite ejecutarse dentro de una transacción):
--   createdb -U postgres vigia_platform
--   psql -U postgres -d vigia_platform -f database/schema.sql
-- =====================================================================

BEGIN;

-- =====================================================================
-- BC-01 · Lead Management (Captación) — Soporte
-- Solicitudes de demostración originadas en el Landing Page.
-- Único contexto anterior a la existencia de relación comercial, por eso
-- no se relaciona con companies.
-- =====================================================================

CREATE TABLE demo_requests (
    id                     uuid         NOT NULL DEFAULT gen_random_uuid(),
    company_name           varchar(150) NOT NULL,
    segment                varchar(30)  NOT NULL,
    contact_full_name      varchar(120) NOT NULL,
    contact_business_email varchar(150) NOT NULL,
    contact_phone_number   varchar(20)  NOT NULL,
    status                 varchar(20)  NOT NULL,
    submitted_at           timestamptz  NOT NULL,
    contacted_at           timestamptz      NULL,

    CONSTRAINT pk_demo_requests        PRIMARY KEY (id),
    CONSTRAINT ck_demo_requests_segment CHECK (segment IN ('CONSTRUCTION_COMPANY', 'LOGISTICS_AND_TRANSPORT')),
    CONSTRAINT ck_demo_requests_status  CHECK (status  IN ('SUBMITTED', 'CONTACTED', 'SCHEDULED', 'DISCARDED'))
);

COMMENT ON TABLE  demo_requests IS 'BC-01 Lead Management. Agregado DemoRequest. VO ContactInfo aplanado con prefijo contact_.';
COMMENT ON COLUMN demo_requests.segment IS 'Segmento objetivo declarado por el visitante (enum TargetSegment).';

-- =====================================================================
-- BC-02 · Identity and Access Management — Soporte
-- Identidad de la organización cliente, sus usuarios y el ciclo de vida
-- de las invitaciones. Contexto del que dependen todos los demás.
-- =====================================================================

CREATE TABLE companies (
    id                  uuid         NOT NULL DEFAULT gen_random_uuid(),
    legal_name          varchar(200) NOT NULL,
    tax_id              varchar(15)  NOT NULL,
    subscription_active boolean      NOT NULL DEFAULT false,
    created_at          timestamptz  NOT NULL DEFAULT now(),

    CONSTRAINT pk_companies        PRIMARY KEY (id),
    CONSTRAINT uq_companies_tax_id UNIQUE (tax_id)
);

COMMENT ON TABLE  companies IS 'BC-02 IAM. Agregado raíz Company. Invariante: una empresa no puede tener usuarios activos si su suscripción no está activa.';
COMMENT ON COLUMN companies.tax_id IS 'RUC de la empresa cliente.';
COMMENT ON COLUMN companies.subscription_active IS 'Proyección del estado que BC-03 comunica de vuelta (relación BC-03 → BC-02).';

CREATE TABLE users (
    id            uuid         NOT NULL DEFAULT gen_random_uuid(),
    company_id    uuid         NOT NULL,
    full_name     varchar(120) NOT NULL,
    email         varchar(150) NOT NULL,
    password_hash varchar(255) NOT NULL,
    role          varchar(30)  NOT NULL,
    is_active     boolean      NOT NULL DEFAULT true,

    CONSTRAINT pk_users            PRIMARY KEY (id),
    CONSTRAINT uq_users_email      UNIQUE (email),
    CONSTRAINT fk_users_companies  FOREIGN KEY (company_id) REFERENCES companies (id) ON DELETE RESTRICT,
    CONSTRAINT ck_users_role       CHECK (role IN ('ADMINISTRATOR', 'RESIDENT_ENGINEER', 'WAREHOUSE_MANAGER',
                                                   'TECHNICAL_OFFICE_MANAGER', 'DISPATCHER', 'CARRIER'))
);

COMMENT ON TABLE users IS 'BC-02 IAM. Agregado raíz User. Invariante: un usuario pertenece a una sola empresa y tiene exactamente un rol.';

CREATE TABLE invitations (
    id            uuid         NOT NULL DEFAULT gen_random_uuid(),
    company_id    uuid         NOT NULL,
    email         varchar(150) NOT NULL,
    role_assigned varchar(30)  NOT NULL,
    status        varchar(20)  NOT NULL DEFAULT 'PENDING',
    sent_at       timestamptz  NOT NULL,
    accepted_at   timestamptz      NULL,

    CONSTRAINT pk_invitations           PRIMARY KEY (id),
    CONSTRAINT fk_invitations_companies FOREIGN KEY (company_id) REFERENCES companies (id) ON DELETE CASCADE,
    CONSTRAINT ck_invitations_role      CHECK (role_assigned IN ('ADMINISTRATOR', 'RESIDENT_ENGINEER', 'WAREHOUSE_MANAGER',
                                                                 'TECHNICAL_OFFICE_MANAGER', 'DISPATCHER', 'CARRIER')),
    CONSTRAINT ck_invitations_status    CHECK (status IN ('PENDING', 'ACCEPTED', 'REVOKED', 'EXPIRED'))
);

COMMENT ON TABLE invitations IS 'BC-02 IAM. Agregado raíz Invitation. Invariante: una invitación pendiente no puede aceptarse dos veces.';

-- =====================================================================
-- BC-03 · Subscriptions and Payment — Soporte
-- Relación comercial entre Trazza Labs y la empresa cliente.
-- company_id referencia BC-02 sin clave foránea (4.8).
-- =====================================================================

CREATE TABLE subscription_plans (
    id                     uuid          NOT NULL DEFAULT gen_random_uuid(),
    tier                   varchar(20)   NOT NULL,
    name                   varchar(80)   NOT NULL,
    max_active_jobsites    integer       NOT NULL,
    max_monthly_dispatches integer       NOT NULL,
    price_amount           numeric(12,2) NOT NULL,
    price_currency         char(3)       NOT NULL,

    CONSTRAINT pk_subscription_plans       PRIMARY KEY (id),
    CONSTRAINT ck_subscription_plans_tier  CHECK (tier IN ('STARTER', 'PROFESSIONAL', 'ENTERPRISE')),
    CONSTRAINT ck_subscription_plans_price CHECK (price_amount >= 0)
);

COMMENT ON TABLE subscription_plans IS 'BC-03 Subscriptions and Payment. VO Money aplanado con prefijo price_.';

CREATE TABLE subscriptions (
    id            uuid        NOT NULL DEFAULT gen_random_uuid(),
    company_id    uuid        NOT NULL,
    plan_id       uuid        NOT NULL,
    start_date    date        NOT NULL,
    renewal_date  date        NOT NULL,
    status        varchar(20) NOT NULL DEFAULT 'PENDING_PAYMENT',

    CONSTRAINT pk_subscriptions                    PRIMARY KEY (id),
    CONSTRAINT fk_subscriptions_subscription_plans FOREIGN KEY (plan_id) REFERENCES subscription_plans (id) ON DELETE RESTRICT,
    CONSTRAINT ck_subscriptions_status             CHECK (status IN ('PENDING_PAYMENT', 'ACTIVE', 'SUSPENDED', 'EXPIRED', 'CANCELLED'))
);

COMMENT ON TABLE  subscriptions IS 'BC-03. Agregado raíz Subscription. Invariante: una empresa tiene como máximo una suscripción vigente.';
COMMENT ON COLUMN subscriptions.company_id IS 'Referencia a companies.id de BC-02. Sin clave foránea, por la separación de contextos (4.8).';

CREATE TABLE payment_transactions (
    id                     uuid          NOT NULL DEFAULT gen_random_uuid(),
    subscription_id        uuid          NOT NULL,
    external_gateway_tx_id varchar(120)  NOT NULL,
    amount_paid_amount     numeric(12,2) NOT NULL,
    amount_paid_currency   char(3)       NOT NULL,
    processed_at           timestamptz   NOT NULL,
    status                 varchar(20)   NOT NULL,
    failure_reason         varchar(300)      NULL,

    CONSTRAINT pk_payment_transactions                 PRIMARY KEY (id),
    CONSTRAINT uq_payment_transactions_external_tx     UNIQUE (external_gateway_tx_id),
    CONSTRAINT fk_payment_transactions_subscriptions   FOREIGN KEY (subscription_id) REFERENCES subscriptions (id) ON DELETE CASCADE,
    CONSTRAINT ck_payment_transactions_status          CHECK (status IN ('PENDING', 'AUTHORIZED', 'REJECTED', 'REFUNDED'))
);

COMMENT ON TABLE  payment_transactions IS 'BC-03. VO Money aplanado con prefijo amount_paid_.';
COMMENT ON COLUMN payment_transactions.external_gateway_tx_id IS 'Identificador de la transacción en la pasarela de pago externa. Único, para idempotencia del webhook de cobro.';

-- =====================================================================
-- BC-04 · Fleet and Device Management — Soporte
-- Unidades de transporte, vinculación del dispositivo GPS y geocercas.
-- vehicles y geofences son agregados independientes: no se relacionan
-- entre sí, la pertenencia a una geocerca se evalúa en el dominio (4.8.1).
-- =====================================================================

CREATE TABLE vehicles (
    id                   uuid             NOT NULL DEFAULT gen_random_uuid(),
    company_id           uuid             NOT NULL,
    license_plate        varchar(10)      NOT NULL,
    brand                varchar(50)      NOT NULL,
    model                varchar(50)      NOT NULL,
    max_capacity_tons    numeric(8,2)     NOT NULL,
    is_gps_paired         boolean          NOT NULL DEFAULT false,
    gps_device_id         varchar(60)          NULL,
    current_pos_latitude  double precision     NULL,
    current_pos_longitude double precision     NULL,

    CONSTRAINT pk_vehicles                PRIMARY KEY (id),
    CONSTRAINT uq_vehicles_license_plate  UNIQUE (license_plate),
    CONSTRAINT uq_vehicles_gps_device_id  UNIQUE (gps_device_id),
    CONSTRAINT ck_vehicles_gps_pairing    CHECK ((is_gps_paired = false AND gps_device_id IS     NULL)
                                              OR (is_gps_paired = true  AND gps_device_id IS NOT NULL))
);

COMMENT ON TABLE  vehicles IS 'BC-04. Agregado raíz Vehicle. Invariante: un dispositivo no puede estar vinculado a más de una unidad al mismo tiempo, garantizada por uq_vehicles_gps_device_id.';
COMMENT ON COLUMN vehicles.gps_device_id IS 'Identificador del dispositivo GPS a bordo. NULL mientras la unidad no tenga dispositivo vinculado. UNIQUE acepta varios NULL en PostgreSQL, por eso no estorba a las unidades sin vincular.';
COMMENT ON COLUMN vehicles.current_pos_latitude IS 'VO Location aplanado con prefijo current_pos_. Última posición conocida de la unidad.';

CREATE TABLE geofences (
    id               uuid             NOT NULL DEFAULT gen_random_uuid(),
    company_id       uuid             NOT NULL,
    name             varchar(120)     NOT NULL,
    type             varchar(20)      NOT NULL,
    center_latitude  double precision NOT NULL,
    center_longitude double precision NOT NULL,
    radius_meters    double precision NOT NULL,

    CONSTRAINT pk_geofences        PRIMARY KEY (id),
    CONSTRAINT ck_geofences_type   CHECK (type IN ('WAREHOUSE', 'JOB_SITE')),
    CONSTRAINT ck_geofences_radius CHECK (radius_meters > 0)
);

COMMENT ON TABLE geofences IS 'BC-04. Agregado raíz Geofence. Invariante: una geocerca pertenece a un único predio, almacén u obra. VO Location aplanado con prefijo center_.';

-- =====================================================================
-- BC-05 · Ordering and Dispatch — CORE
-- Desde la solicitud de materiales hasta la salida de la unidad del
-- almacén central. Aquí se origina el registro de custodia.
-- =====================================================================

CREATE TABLE orders (
    id                   uuid        NOT NULL DEFAULT gen_random_uuid(),
    site_id              uuid        NOT NULL,
    requested_by_user_id uuid        NOT NULL,
    order_date           timestamptz NOT NULL,
    status               varchar(25) NOT NULL DEFAULT 'PENDING',

    CONSTRAINT pk_orders        PRIMARY KEY (id),
    CONSTRAINT ck_orders_status CHECK (status IN ('PENDING', 'PARTIALLY_ATTENDED', 'COMPLETELY_ATTENDED', 'CANCELLED'))
);

COMMENT ON TABLE  orders IS 'BC-05. Agregado raíz Order. Invariante: un pedido no puede quedar en atención total si algún material tiene existencia insuficiente.';
COMMENT ON COLUMN orders.site_id IS 'Obra de destino. Referencia externa sin clave foránea.';
COMMENT ON COLUMN orders.requested_by_user_id IS 'Referencia a users.id de BC-02. Sin clave foránea.';

CREATE TABLE order_items (
    id                 uuid          NOT NULL DEFAULT gen_random_uuid(),
    order_id           uuid          NOT NULL,
    material_name      varchar(150)  NOT NULL,
    unit_of_measure    varchar(15)   NOT NULL,
    requested_quantity numeric(12,3) NOT NULL,
    assigned_quantity  numeric(12,3) NOT NULL DEFAULT 0,

    CONSTRAINT pk_order_items             PRIMARY KEY (id),
    CONSTRAINT fk_order_items_orders      FOREIGN KEY (order_id) REFERENCES orders (id) ON DELETE CASCADE,
    CONSTRAINT ck_order_items_quantities  CHECK (assigned_quantity <= requested_quantity)
);

COMMENT ON TABLE order_items IS 'BC-05. Entidad hija de Order (1..*). La cobertura parcial se expresa con assigned_quantity < requested_quantity.';

CREATE TABLE dispatches (
    id                            uuid          NOT NULL DEFAULT gen_random_uuid(),
    order_id                      uuid          NOT NULL,
    vehicle_id                    uuid          NOT NULL,
    carrier_id                    uuid          NOT NULL,
    estimated_weight_kg           numeric(12,2) NOT NULL,
    scale_weight_kg               numeric(12,2) NOT NULL,
    status                        varchar(30)   NOT NULL DEFAULT 'DRAFT',
    departed_at                   timestamptz       NULL,
    waybill_number                varchar(40)       NULL,
    waybill_issued_at             timestamptz       NULL,
    waybill_authorized_by_user_id uuid              NULL,

    CONSTRAINT pk_dispatches        PRIMARY KEY (id),
    CONSTRAINT fk_dispatches_orders FOREIGN KEY (order_id) REFERENCES orders (id) ON DELETE RESTRICT,
    CONSTRAINT ck_dispatches_status CHECK (status IN ('DRAFT', 'WEIGHT_CONFIRMED', 'WEIGHT_FLAGGED_FOR_REVIEW',
                                                      'WAYBILL_ISSUED', 'DEPARTED'))
);

COMMENT ON TABLE  dispatches IS 'BC-05. Agregado raíz Dispatch. Invariante: no puede emitirse la guía de remisión sin pesaje registrado. VO DigitalWaybill aplanado con prefijo waybill_, nullable porque el despacho existe antes de emitirse la guía.';
COMMENT ON COLUMN dispatches.vehicle_id IS 'Referencia a vehicles.id de BC-04. Sin clave foránea.';

-- =====================================================================
-- BC-06 · Transit Traceability — CORE
-- Traslado de la unidad desde la salida del almacén hasta la llegada a
-- obra. Único contexto sin rol humano: todo entra por el webhook de
-- telemetría. El agregado Route nace en BC-05 y aquí se enriquece.
-- =====================================================================

CREATE TABLE routes (
    id             uuid        NOT NULL DEFAULT gen_random_uuid(),
    dispatch_id    uuid        NOT NULL,
    vehicle_id     uuid        NOT NULL,
    started_at     timestamptz NOT NULL,
    completed_at   timestamptz     NULL,
    current_status varchar(25) NOT NULL DEFAULT 'IN_TRANSIT',

    CONSTRAINT pk_routes        PRIMARY KEY (id),
    CONSTRAINT ck_routes_status CHECK (current_status IN ('IN_TRANSIT', 'DEVIATION_ALERT', 'STOPPED', 'COMPLETED'))
);

COMMENT ON TABLE routes IS 'BC-06. Agregado raíz Route. Puente entre BC-05 y BC-06: se abre al registrar la salida de la unidad.';

CREATE TABLE position_reports (
    id          bigint           NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    route_id    uuid             NOT NULL,
    latitude    double precision NOT NULL,
    longitude   double precision NOT NULL,
    speed_kmh   double precision NOT NULL,
    occurred_at timestamptz      NOT NULL,
    received_at timestamptz      NOT NULL DEFAULT now(),

    CONSTRAINT pk_position_reports        PRIMARY KEY (id),
    CONSTRAINT fk_position_reports_routes FOREIGN KEY (route_id) REFERENCES routes (id) ON DELETE CASCADE
);

COMMENT ON TABLE  position_reports IS 'BC-06. VO PositionReport con clave técnica propia por presentarse como colección. Clave entera por volumen: recibe un registro por cada reporte del dispositivo GPS.';
COMMENT ON COLUMN position_reports.occurred_at IS 'Momento informado por el dispositivo a bordo.';
COMMENT ON COLUMN position_reports.received_at IS 'Momento de recepción en el servidor. Conservar ambas marcas sostiene el valor probatorio de la evidencia (4.6.3).';

CREATE TABLE route_incidents (
    id            uuid         NOT NULL DEFAULT gen_random_uuid(),
    route_id      uuid         NOT NULL,
    incident_type varchar(30)  NOT NULL,
    description   varchar(300) NOT NULL,
    detected_at   timestamptz  NOT NULL,
    is_resolved   boolean      NOT NULL DEFAULT false,

    CONSTRAINT pk_route_incidents        PRIMARY KEY (id),
    CONSTRAINT fk_route_incidents_routes FOREIGN KEY (route_id) REFERENCES routes (id) ON DELETE CASCADE,
    CONSTRAINT ck_route_incidents_type   CHECK (incident_type IN ('PROLONGED_STOP', 'UNAUTHORIZED_DEVIATION',
                                                                  'GPS_SIGNAL_LOST', 'GPS_SIGNAL_RESTORED'))
);

COMMENT ON TABLE route_incidents IS 'BC-06. Anomalías detectadas en ruta por los procesos de negocio del contexto.';

-- =====================================================================
-- BC-07 · Site Reception and Verification — CORE
-- Recepción física en obra y cotejo contra la guía de remisión. Aquí el
-- registro de custodia se cierra o se bifurca hacia una discrepancia.
-- =====================================================================

CREATE TABLE receptions (
    id                  uuid        NOT NULL DEFAULT gen_random_uuid(),
    dispatch_id         uuid        NOT NULL,
    verified_by_user_id uuid        NOT NULL,
    arrived_at          timestamptz NOT NULL,
    closed_at           timestamptz     NULL,
    status              varchar(25) NOT NULL DEFAULT 'ARRIVED',

    CONSTRAINT pk_receptions           PRIMARY KEY (id),
    CONSTRAINT uq_receptions_dispatch  UNIQUE (dispatch_id),
    CONSTRAINT ck_receptions_status    CHECK (status IN ('ARRIVED', 'VERIFIED_CONFORMANT', 'VERIFIED_DISCREPANT'))
);

COMMENT ON TABLE  receptions IS 'BC-07. Agregado raíz Reception. Invariante: una recepción se valida siempre contra la guía de remisión de un despacho existente.';
COMMENT ON COLUMN receptions.dispatch_id IS 'Referencia a dispatches.id de BC-05, sin clave foránea. UNIQUE: un despacho se recibe una sola vez.';

CREATE TABLE reception_items (
    id                  uuid          NOT NULL DEFAULT gen_random_uuid(),
    reception_id        uuid          NOT NULL,
    material_name       varchar(150)  NOT NULL,
    dispatched_quantity numeric(12,3) NOT NULL,
    received_quantity   numeric(12,3) NOT NULL,

    CONSTRAINT pk_reception_items            PRIMARY KEY (id),
    CONSTRAINT fk_reception_items_receptions FOREIGN KEY (reception_id) REFERENCES receptions (id) ON DELETE CASCADE,
    CONSTRAINT ck_reception_items_quantities CHECK (dispatched_quantity >= 0 AND received_quantity >= 0)
);

COMMENT ON TABLE reception_items IS 'BC-07. Entidad hija de Reception (1..*). La diferencia no se almacena: se obtiene de dispatched_quantity - received_quantity (4.8.1).';

CREATE TABLE supporting_evidences (
    id           uuid         NOT NULL DEFAULT gen_random_uuid(),
    reception_id uuid         NOT NULL,
    evidence_url varchar(500) NOT NULL,
    captured_at  timestamptz  NOT NULL,
    caption      varchar(300) NOT NULL,

    CONSTRAINT pk_supporting_evidences            PRIMARY KEY (id),
    CONSTRAINT fk_supporting_evidences_receptions FOREIGN KEY (reception_id) REFERENCES receptions (id) ON DELETE CASCADE
);

COMMENT ON TABLE supporting_evidences IS 'BC-07. VO SupportingEvidence. Registro fotográfico adjuntado por el Encargado de Obra.';

-- =====================================================================
-- BC-08 · Discrepancy and Evidence Management — CORE
-- Expediente del caso, consolidación de evidencia de los tres tramos,
-- reporte de sustento, determinación de responsabilidad y cierre.
-- La trazabilidad se resuelve POR COPIA, no por referencia: los datos
-- quedan fijados al consolidar y no se alteran si cambia el origen (4.8.1).
-- =====================================================================

CREATE TABLE discrepancy_cases (
    id                   uuid        NOT NULL DEFAULT gen_random_uuid(),
    dispatch_id          uuid        NOT NULL,
    reception_id         uuid        NOT NULL,
    case_number          varchar(30) NOT NULL,
    opened_at            timestamptz NOT NULL,
    closed_at            timestamptz     NULL,
    status               varchar(25) NOT NULL DEFAULT 'OPEN',
    determined_liability varchar(20) NOT NULL DEFAULT 'PENDING',
    resolution_summary   text            NULL,

    CONSTRAINT pk_discrepancy_cases            PRIMARY KEY (id),
    CONSTRAINT uq_discrepancy_cases_number     UNIQUE (case_number),
    CONSTRAINT ck_discrepancy_cases_status     CHECK (status IN ('OPEN', 'UNDER_REVIEW', 'REPORT_GENERATED', 'CLOSED')),
    CONSTRAINT ck_discrepancy_cases_liability  CHECK (determined_liability IN ('PENDING', 'SUPPLIER', 'CARRIER',
                                                                               'WAREHOUSE', 'JOB_SITE', 'NORMAL_LOSS'))
);

COMMENT ON TABLE  discrepancy_cases IS 'BC-08. Agregado raíz DiscrepancyCase. Invariante: un caso no puede cerrarse sin una responsabilidad determinada (determined_liability <> PENDING), validada en el dominio.';
COMMENT ON COLUMN discrepancy_cases.determined_liability IS 'Arranca en PENDING al abrirse el caso automáticamente desde el evento "Discrepancia reportada" de BC-07.';

CREATE TABLE consolidated_timeline_entries (
    id                bigint       NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    case_id           uuid         NOT NULL,
    entry_timestamp   timestamptz  NOT NULL,
    source_context    varchar(50)  NOT NULL,
    event_description varchar(300) NOT NULL,

    CONSTRAINT pk_consolidated_timeline_entries                   PRIMARY KEY (id),
    CONSTRAINT fk_consolidated_timeline_entries_discrepancy_cases FOREIGN KEY (case_id) REFERENCES discrepancy_cases (id) ON DELETE CASCADE
);

COMMENT ON TABLE  consolidated_timeline_entries IS 'BC-08. VO ConsolidatedTimelineEntry con clave técnica propia por presentarse como colección.';
COMMENT ON COLUMN consolidated_timeline_entries.source_context IS 'Bounded Context de origen de la entrada. Permite reunir evidencia de BC-05, BC-06 y BC-07 sin claves foráneas hacia ellos.';
COMMENT ON COLUMN consolidated_timeline_entries.entry_timestamp IS 'Se nombra entry_timestamp y no timestamp porque este último es palabra reservada en SQL. Única denominación que se aparta de 4.7.1.';

-- =====================================================================
-- Índices
-- =====================================================================

-- Especificados en la sección 4.8.1
CREATE INDEX idx_position_reports_route_occurred ON position_reports (route_id, occurred_at);
CREATE INDEX idx_timeline_case_timestamp         ON consolidated_timeline_entries (case_id, entry_timestamp);

-- Índices sobre columnas de clave foránea y de referencia entre contextos.
-- PostgreSQL no los crea automáticamente al declarar una FOREIGN KEY, y
-- son los caminos de acceso que usan los modelos de lectura de 4.6.1.
CREATE INDEX idx_users_company                   ON users (company_id);
CREATE INDEX idx_invitations_company             ON invitations (company_id);
CREATE INDEX idx_subscriptions_company           ON subscriptions (company_id);
CREATE INDEX idx_subscriptions_plan              ON subscriptions (plan_id);
CREATE INDEX idx_payment_transactions_subscription ON payment_transactions (subscription_id);
CREATE INDEX idx_vehicles_company                ON vehicles (company_id);
CREATE INDEX idx_geofences_company               ON geofences (company_id);
CREATE INDEX idx_orders_site                     ON orders (site_id);
CREATE INDEX idx_orders_requested_by             ON orders (requested_by_user_id);
CREATE INDEX idx_order_items_order               ON order_items (order_id);
CREATE INDEX idx_dispatches_order                ON dispatches (order_id);
CREATE INDEX idx_dispatches_vehicle              ON dispatches (vehicle_id);
CREATE INDEX idx_routes_dispatch                 ON routes (dispatch_id);
CREATE INDEX idx_routes_vehicle                  ON routes (vehicle_id);
CREATE INDEX idx_route_incidents_route           ON route_incidents (route_id);
CREATE INDEX idx_reception_items_reception       ON reception_items (reception_id);
CREATE INDEX idx_supporting_evidences_reception  ON supporting_evidences (reception_id);
CREATE INDEX idx_discrepancy_cases_dispatch      ON discrepancy_cases (dispatch_id);
CREATE INDEX idx_discrepancy_cases_reception     ON discrepancy_cases (reception_id);

COMMIT;
