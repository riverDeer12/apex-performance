use ApexPerformance

go

-- CREATING SUPERADMIN
INSERT INTO Users (
    Id,
    CreatedAt,
    CreatedBy,
    UpdatedAt,
    UpdatedBy,
    IsDeleted,
    DeletedAt,
    UserName,
    Email,
    Password,
    EmailConfirmed
) VALUES (
    '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
    CURRENT_TIMESTAMP,
    '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
    CURRENT_TIMESTAMP,
    '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
    0,
    NULL,
    'superadmin',
    'superadmin@apex-performance.fit',
    '685D8127992F8280BB94EC3CF3F2B4DA35904A8AE09AC07AF245D1888A620FAF97DE8084F4141D5F2107BEB09FC7F57073EAE8746A000A0DFFD507C79ED055A3',
    1
);

-- CREATING SUPERADMIN ROLE
INSERT INTO Roles (
    Id,
    CreatedAt,
    CreatedBy,
    UpdatedAt,
    UpdatedBy,
    IsDeleted,
    DeletedAt,
    Name,
    Description
) VALUES (
    '69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f',
    CURRENT_TIMESTAMP,
    '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
    CURRENT_TIMESTAMP,
    '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
    0,
    NULL,
    'SuperAdmin',
    'Role with all access.'
);

-- CREATING RELATIONSHIPS
INSERT INTO UserRoles (
    UserId,
    RoleId
) VALUES (
    '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
    '69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f'
);

-- CREATING ADMINISTRATOR ACCOUNT FOR SUPER ADMIN
INSERT INTO Administrators (
    Id,
    CreatedAt,
    CreatedBy,
    UpdatedAt,
    UpdatedBy,
    IsDeleted,
    DeletedAt,
    UserId,
    FirstName,
    LastName
) VALUES (
    'db469ddd-cce5-4aaa-9911-f8610d1cb08a', -- Replace with actual GUID if needed
    CURRENT_TIMESTAMP,
    '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
    CURRENT_TIMESTAMP,
    '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
    0,
    NULL,
    '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
    'Super',
    'Admin'
);

-- CREATING PERMISSIONS
INSERT INTO permissions (id, name, description, category) VALUES
-- Appointments
('c1a4f730-9c92-47c0-97b0-9ce7e94fc20a', 'CanGetAppointments', 'Allows viewing appointments.', 'Appointments'),
('11c09349-6f32-4a43-a4b2-dbd58c244b1a', 'CanCreateAppointment', 'Allows creating appointment.', 'Appointments'),
('f9fa3e15-8819-48f0-8751-02cf42e22a1d', 'CanUpdateAppointment', 'Allows updating appointment.', 'Appointments'),
('6a6c1fd6-8c28-49cf-8a71-91bead303a6f', 'CanDeleteAppointment', 'Allows deleting appointment.', 'Appointments'),
('5a0c2c49-6e9a-4c45-a6db-d50f802816ef', 'CanApproveAppointment', 'Allows approving appointment.', 'Appointments'),
('3a3f30a7-c0fc-43e7-aac0-861a53836479', 'CanDeclineAppointment', 'Allows declining appointment.', 'Appointments'),
('b8f6cb70-ccf7-4a1c-8e6a-3f4a13c2735d', 'CanProgressAppointment', 'Allows progressing appointment.', 'Appointments'),
('fea29130-f5c3-465e-b09e-cd5828a7071f', 'CanCancelAppointment', 'Allows canceling appointment.', 'Appointments'),

-- Body Measurements
('3f8c91de-49e7-4af7-b3f4-8d5c7ff1a9ae', 'CanGetBodyMeasurements', 'Allows viewing body measurements.', 'BodyMeasurements'),
('c7a1738d-e20f-4931-a2f1-d3c0dcf64c5f', 'CanCreateBodyMeasurement', 'Allows creating body measurements.', 'BodyMeasurements'),
('a412e56f-5d9c-4e1d-97e4-1c31f7aa2e59', 'CanUpdateBodyMeasurement', 'Allows updating body measurements.', 'BodyMeasurements'),
('8b13e0cb-4c27-497f-bf13-b2101d8f0efb', 'CanDeleteBodyMeasurement', 'Allows deleting body measurements.', 'BodyMeasurements'),

-- Clients
('17ef9141-208a-491a-9cb7-84d4f8375fb9', 'CanGetClients', 'Allows viewing clients.', 'Clients'),
('28ab969b-c866-470f-b3b4-7c1f1b066a48', 'CanCreateClient', 'Allows creating client.', 'Clients'),
('718efa10-761a-4e44-8eda-eae6db4cb0a3', 'CanUpdateClient', 'Allows updating client.', 'Clients'),
('b22c672f-6d69-4674-b9cd-5fbb8d497e7a', 'CanDeleteClient', 'Allows deleting client.', 'Clients'),

-- Coaches
('3c9e9e34-7a7f-4e49-8f3d-2c7a1f3c6b22', 'CanGetCoaches', 'Allows viewing coaches.', 'Coaches'),
('d8e0e377-5f70-4f5b-a9bc-68a7b91c5a8d', 'CanCreateCoach', 'Allows creating coach.', 'Coaches'),
('7aeb7c60-844b-4a38-b1ae-55829b8e5f3a', 'CanUpdateCoach', 'Allows updating coach.', 'Coaches'),
('e3b4a1d2-f0fa-4d56-b349-8bb7b78f99ff', 'CanDeleteCoach', 'Allows deleting coach.', 'Coaches')

-- CREATE APPOINTMENT STATUSES

INSERT INTO AppointmentStatuses (id, name, description) VALUES
('9f1a0b47-1f9d-4a6d-bcfc-5ed5e91cdaf7', 'Approved', 'Approved status.'),
('4f82c9d9-3346-4c53-8d36-2f6e7186e1a3', 'Declined', 'Declined status.'),
('6a79d224-c6a5-4f14-90ad-ec9e7a2743c2', 'InProgress', 'In Progress status.'),
('27c2b8b8-d95d-4e35-8df6-90f8b31a22aa', 'Pending', 'Pending status.'),
('e2031af4-e2d7-440d-a88b-b7e09fff9805', 'Canceled', 'Canceled status.');



