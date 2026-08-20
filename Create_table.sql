
-- Полная очистка и подготовка
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;
SET search_path TO public;

-- 1. Роли
CREATE TABLE roles (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL
);

-- 2. Позиции
CREATE TABLE positions (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL
);

-- 3. Статусы сотрудников
CREATE TABLE employee_states (
    id BIGSERIAL PRIMARY KEY,
    state VARCHAR(255) NOT NULL
);

-- 4. Телефоны
CREATE TABLE phone_numbers (
    id BIGSERIAL PRIMARY KEY,
    phone VARCHAR(50) NOT NULL,
    description VARCHAR(255) NOT NULL
);

-- 5. Люди 
CREATE TABLE peoples (
    id BIGSERIAL PRIMARY KEY,
    surname VARCHAR(255) NOT NULL,
    name VARCHAR(255) NOT NULL,
    patronymic VARCHAR(255),
    phone_number_id BIGINT,
    email VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    CONSTRAINT fk_peoples_phone FOREIGN KEY (phone_number_id) REFERENCES phone_numbers(id)
);

-- 6. Контрагенты
CREATE TABLE contragents (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255),
    address VARCHAR(255)
);

-- 7. Клиенты 
CREATE TABLE clients (
    id BIGSERIAL PRIMARY KEY,
    contragents_id BIGINT,
    people_id BIGINT NOT NULL,
    CONSTRAINT fk_clients_contragent FOREIGN KEY (contragents_id) REFERENCES contragents(id),
    CONSTRAINT fk_clients_people FOREIGN KEY (people_id) REFERENCES peoples(id)
);

-- 8. Сотрудники 
CREATE TABLE employees (
    id BIGSERIAL PRIMARY KEY,
    people_id BIGINT NOT NULL,
    position_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    employee_state_id BIGINT NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL, 
    CONSTRAINT fk_employees_people FOREIGN KEY (people_id) REFERENCES peoples(id),
    CONSTRAINT fk_employees_position FOREIGN KEY (position_id) REFERENCES positions(id),
    CONSTRAINT fk_employees_role FOREIGN KEY (role_id) REFERENCES roles(id),
    CONSTRAINT fk_employees_state FOREIGN KEY (employee_state_id) REFERENCES employee_states(id)
);

-- 9. Статусы объектов
CREATE TABLE object_statuses (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL
);

-- 10. Статусы договоров
CREATE TABLE contract_statuses (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL
);

-- 11. Статусы заявок
CREATE TABLE application_statuses (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL
);

-- 12. Статусы задач
CREATE TABLE task_statuses (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL
);

-- 13. Услуги
CREATE TABLE services (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    unit VARCHAR(50),
    base_price NUMERIC(18,2) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE
);

-- 14. Шаблоны договоров
CREATE TABLE contract_templates (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    version VARCHAR(20) DEFAULT '1.0',
    file_path VARCHAR(255) NOT NULL,
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by_employee_id BIGINT NOT NULL,
    CONSTRAINT fk_temp_creator FOREIGN KEY (created_by_employee_id) REFERENCES employees(id)
);

-- 15. Договоры
CREATE TABLE contracts (
    id BIGSERIAL PRIMARY KEY,
    client_id BIGINT NOT NULL,
    template_id BIGINT NOT NULL,
    status_id BIGINT NOT NULL,
    created_by_employee_id BIGINT NOT NULL,
    updated_by_employee_id BIGINT,
    file_path TEXT,
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP,
    termination_reason TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    CONSTRAINT fk_contracts_client FOREIGN KEY (client_id) REFERENCES clients(id),
    CONSTRAINT fk_contracts_template FOREIGN KEY (template_id) REFERENCES contract_templates(id),
    CONSTRAINT fk_contracts_status FOREIGN KEY (status_id) REFERENCES contract_statuses(id),
    CONSTRAINT fk_contracts_creator FOREIGN KEY (created_by_employee_id) REFERENCES employees(id),
    CONSTRAINT fk_contracts_updater FOREIGN KEY (updated_by_employee_id) REFERENCES employees(id)
);

-- 16. Объекты недвижимости
CREATE TABLE objects (
    id BIGSERIAL PRIMARY KEY,
    address VARCHAR(255) NOT NULL,
    project_description TEXT,
    current_status_id BIGINT NOT NULL,
    manager_employee_id BIGINT,
    contract_id BIGINT,
    CONSTRAINT fk_obj_status FOREIGN KEY (current_status_id) REFERENCES object_statuses(id),
    CONSTRAINT fk_obj_manager FOREIGN KEY (manager_employee_id) REFERENCES employees(id),
    CONSTRAINT fk_obj_contract FOREIGN KEY (contract_id) REFERENCES contracts(id)
);

-- 17. Документы 
CREATE TABLE documents (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    file_path VARCHAR(255) NOT NULL,
    upload_date TIMESTAMP NOT NULL DEFAULT NOW(),
    uploaded_by_employee_id BIGINT NOT NULL,
    object_id BIGINT NOT NULL,
    description TEXT,
    CONSTRAINT fk_docs_employee FOREIGN KEY (uploaded_by_employee_id) REFERENCES employees(id),
    CONSTRAINT fk_docs_object FOREIGN KEY (object_id) REFERENCES objects(id)
);

-- 18. Задачи 
CREATE TABLE tasks (
    id BIGSERIAL PRIMARY KEY,
    parent_task_id BIGINT,
    object_id BIGINT NOT NULL,
    employee_id BIGINT NOT NULL,
    status_id BIGINT NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    planned_start_date TIMESTAMP,
    planned_end_date TIMESTAMP,
    actual_start_date TIMESTAMP,
    actual_end_date TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    CONSTRAINT fk_tasks_parent FOREIGN KEY (parent_task_id) REFERENCES tasks(id),
    CONSTRAINT fk_tasks_object FOREIGN KEY (object_id) REFERENCES objects(id),
    CONSTRAINT fk_tasks_employee FOREIGN KEY (employee_id) REFERENCES employees(id),
    CONSTRAINT fk_tasks_status FOREIGN KEY (status_id) REFERENCES task_statuses(id)
);

-- 19. Заявки 
CREATE TABLE applications (
    id BIGSERIAL PRIMARY KEY,
    client_id BIGINT NOT NULL,
    status_id BIGINT NOT NULL,
    assigned_manager_id BIGINT,
    admin_comment TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by_employee_id BIGINT NOT NULL,
    updated_by_employee_id BIGINT NOT NULL,
    CONSTRAINT fk_apps_client FOREIGN KEY (client_id) REFERENCES clients(id),
    CONSTRAINT fk_apps_status FOREIGN KEY (status_id) REFERENCES application_statuses(id),
    CONSTRAINT fk_apps_manager FOREIGN KEY (assigned_manager_id) REFERENCES employees(id),
    CONSTRAINT fk_apps_creator FOREIGN KEY (created_by_employee_id) REFERENCES employees(id),
    CONSTRAINT fk_apps_updater FOREIGN KEY (updated_by_employee_id) REFERENCES employees(id)
);

-- 20. Связь заявки и услуг
CREATE TABLE application_services (
    id BIGSERIAL PRIMARY KEY,
    application_id BIGINT NOT NULL,
    service_id BIGINT NOT NULL,
    quantity NUMERIC(18,2) NOT NULL,
    price_per_unit NUMERIC(18,2) NOT NULL,
    total_price NUMERIC(18,2) NOT NULL,
    CONSTRAINT fk_app_svc_app FOREIGN KEY (application_id) REFERENCES applications(id),
    CONSTRAINT fk_app_svc_service FOREIGN KEY (service_id) REFERENCES services(id)
);

-- 21. История статусов заявок 
CREATE TABLE application_status_history (
    id BIGSERIAL PRIMARY KEY,
    application_id BIGINT NOT NULL,
    status_id BIGINT,
    changed_by_employee_id BIGINT NOT NULL,
    change_comment TEXT,
    changed_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_hist_app FOREIGN KEY (application_id) REFERENCES applications(id),
    CONSTRAINT fk_hist_status FOREIGN KEY (status_id) REFERENCES application_statuses(id),
    CONSTRAINT fk_hist_changer FOREIGN KEY (changed_by_employee_id) REFERENCES employees(id)
);

-- 22. Иерархия сотрудников
CREATE TABLE employees_hierarchy (
    id BIGSERIAL PRIMARY KEY,
    supervisor_employee_id BIGINT NOT NULL,
    subordinate_employee_id BIGINT NOT NULL,
    CONSTRAINT fk_emp_hier_supervisor FOREIGN KEY (supervisor_employee_id) REFERENCES employees(id),
    CONSTRAINT fk_emp_hier_subordinate FOREIGN KEY (subordinate_employee_id) REFERENCES employees(id)
);

-- 23. История менеджеров объектов
CREATE TABLE managers_history (
    id BIGSERIAL PRIMARY KEY,
    object_id BIGINT NOT NULL,
    manager_employee_id BIGINT NOT NULL,
    assigned_by_employee_id BIGINT NOT NULL,
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP,
    CONSTRAINT fk_mgr_hist_obj FOREIGN KEY (object_id) REFERENCES objects(id),
    CONSTRAINT fk_mgr_hist_mgr FOREIGN KEY (manager_employee_id) REFERENCES employees(id),
    CONSTRAINT fk_mgr_hist_assigner FOREIGN KEY (assigned_by_employee_id) REFERENCES employees(id)
);

-- 24. Очередь писем
CREATE TABLE email_queue (
    id BIGSERIAL PRIMARY KEY,
    recipient_email VARCHAR(255) NOT NULL,
    subject VARCHAR(255) NOT NULL,
    body TEXT,
    send_status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    sent_at TIMESTAMP,
    error_message VARCHAR(1000),
    created_by_employee_id BIGINT,
    CONSTRAINT fk_email_creator FOREIGN KEY (created_by_employee_id) REFERENCES employees(id)
);

-- 1. Заполнение справочников 

-- Роли
INSERT INTO roles (name) VALUES 
('Администратор'),
('Руководитель'),
('Сотрудник');

-- Позиции
INSERT INTO positions (name) VALUES 
('Генеральный директор'),
('Главный инженер'),
('Прораб'),
('Мастер участка'),
('Каменщик'),
('Бетонщик'),
('Кровельщик');

-- Статусы сотрудников
INSERT INTO employee_states (state) VALUES 
('Активен'),
('В отпуске'),
('Уволен'),
('На испытательном сроке');

-- Статусы объектов
INSERT INTO object_statuses (name) VALUES 
('Проектирование'),
('Подготовка площадки'),
('Фундамент'),
('Коробка здания'),
('Кровля'),
('Отделка'),
('Сдача в эксплуатацию'),
('Архив (завершено)'),
('Приостановлен');

-- Статусы договоров
INSERT INTO contract_statuses (name) VALUES 
('Черновик'),
('На согласовании'),
('Подписан'),
('Исполняется'),
('Расторгнут'),
('Закрыт');

-- Статусы заявок
INSERT INTO application_statuses (name) VALUES 
('Новая'),
('В работе'),
('Требуется уточнение'),
('Отклонена'),
('Передана менеджеру'),
('Договор заключен'),
('Выполнена');

-- Статусы задач
INSERT INTO task_statuses (name) VALUES 
('Не начата'),
('В работе'),
('Требует проверки'),
('Готово'),
('Отменена');

-- Услуги (для калькулятора и заявок)
INSERT INTO services (name, description, unit, base_price, is_active) VALUES 
('По договоренности', 'В процессе', '', 0, true),
('Заливка фундамента', 'Бетонирование основания здания', 'м3', 4500.00, true),
('Кладка стен', 'Возведение несущих стен из кирпича/блока', 'м2', 3200.00, true),
('Монтаж кровли', 'Устройство крыши с гидроизоляцией', 'м2', 2800.00, true),
('Штукатурные работы', 'Выравнивание стен под отделку', 'м2', 650.00, true),
('Укладка плитки', 'Облицовка санузлов и полов', 'м2', 1200.00, true),
('Электромонтаж', 'Прокладка проводки и установка щитов', 'точка', 400.00, true);


-- Телефоны
INSERT INTO phone_numbers (phone, description) VALUES 
('+79990000001', 'Рабочий телефон офиса'),
('+79990000002', 'Мобильный директора'),
('+79990000003', 'Мобильный прораба Иванова'),
('+79990000004', 'Мобильный каменщика Петрова'),
('+79990000005', 'Телефон для заявок');





