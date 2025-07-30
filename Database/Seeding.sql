use [apex-performance]

go

-- CREATING SUPER ADMIN
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
    
-- CREATING APEX ADMIN
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
             'd9f1c2a4-5b3e-4ea3-91e1-b3a194b4e6af',
             CURRENT_TIMESTAMP,
             '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
             CURRENT_TIMESTAMP,
             '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
             0,
             NULL,
             'apex',
             'apex@apex-performance.fit',
             'AAE7DA59D5D8DAE54605DC0E9D6F42AD188B99B0E872351DBBCF6DA12A09148A683DDB52043EABFDAA2AE08904FAD2B7E4B2E6F93C8E9E37B509A7769438A9E3',
             1
         );

-- CREATING ROLES FOR SUPER ADMIN, ADMIN, COACH, CLIENT
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
             '63526fbb-b013-4a26-8c17-b164cc7903fa',
             CURRENT_TIMESTAMP,
             '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
             CURRENT_TIMESTAMP,
             '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
             0,
             NULL,
             'Administrator',
             'Highest role for Apex Performance app.'
         );

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
             '263f820c-5f83-4228-97e1-30e52f51b4ca',
             CURRENT_TIMESTAMP,
             '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
             CURRENT_TIMESTAMP,
             '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
             0,
             NULL,
             'Coach',
             'Basic coach role.'
         );

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
             'd12f1605-c1d1-4a81-bd27-ca118b1b55a5',
             CURRENT_TIMESTAMP,
             '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
             CURRENT_TIMESTAMP,
             '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
             0,
             NULL,
             'Client',
             'Basic client role.'
         );

-- CREATING ROLE RELATIONSHIP FOR SUPER ADMIN
INSERT INTO UserRoles (
    UserId,
    RoleId
) VALUES (
             '5604e898-cd94-476b-8b86-9aa3a87cc9bb',
             '69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f'
         );

-- CREATING ROLE RELATIONSHIP FOR ADMIN
INSERT INTO UserRoles (
    UserId,
    RoleId
) VALUES (
             'd9f1c2a4-5b3e-4ea3-91e1-b3a194b4e6af',
             '63526fbb-b013-4a26-8c17-b164cc7903fa'
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
             'db469ddd-cce5-4aaa-9911-f8610d1cb08a',
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

-- CREATING PERMISSIONS RELATIONSHIPS FOR CLIENT AND COACH ROLES
    INSERT INTO RolePermissions (RoleId, PermissionId) VALUES
-- Appointment's Permissions
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', 'c1a4f730-9c92-47c0-97b0-9ce7e94fc20a'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', 'c1a4f730-9c92-47c0-97b0-9ce7e94fc20a'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', '11c09349-6f32-4a43-a4b2-dbd58c244b1a'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', '11c09349-6f32-4a43-a4b2-dbd58c244b1a'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', 'f9fa3e15-8819-48f0-8751-02cf42e22a1d'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', 'f9fa3e15-8819-48f0-8751-02cf42e22a1d'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', '6a6c1fd6-8c28-49cf-8a71-91bead303a6f'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', '6a6c1fd6-8c28-49cf-8a71-91bead303a6f'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', '5a0c2c49-6e9a-4c45-a6db-d50f802816ef'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', '5a0c2c49-6e9a-4c45-a6db-d50f802816ef'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', '3a3f30a7-c0fc-43e7-aac0-861a53836479'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', '3a3f30a7-c0fc-43e7-aac0-861a53836479'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', 'b8f6cb70-ccf7-4a1c-8e6a-3f4a13c2735d'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', 'b8f6cb70-ccf7-4a1c-8e6a-3f4a13c2735d'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', 'fea29130-f5c3-465e-b09e-cd5828a7071f'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', 'fea29130-f5c3-465e-b09e-cd5828a7071f'),

-- Body Measurement's Permissions
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', '3f8c91de-49e7-4af7-b3f4-8d5c7ff1a9ae'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', '3f8c91de-49e7-4af7-b3f4-8d5c7ff1a9ae'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', 'c7a1738d-e20f-4931-a2f1-d3c0dcf64c5f'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', 'c7a1738d-e20f-4931-a2f1-d3c0dcf64c5f'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', 'a412e56f-5d9c-4e1d-97e4-1c31f7aa2e59'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', 'a412e56f-5d9c-4e1d-97e4-1c31f7aa2e59'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', '8b13e0cb-4c27-497f-bf13-b2101d8f0efb'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', '8b13e0cb-4c27-497f-bf13-b2101d8f0efb'),

-- Client's Permissions
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', '17ef9141-208a-491a-9cb7-84d4f8375fb9'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', '17ef9141-208a-491a-9cb7-84d4f8375fb9'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', '28ab969b-c866-470f-b3b4-7c1f1b066a48'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', '28ab969b-c866-470f-b3b4-7c1f1b066a48'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', '718efa10-761a-4e44-8eda-eae6db4cb0a3'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', '718efa10-761a-4e44-8eda-eae6db4cb0a3'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', 'b22c672f-6d69-4674-b9cd-5fbb8d497e7a'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', 'b22c672f-6d69-4674-b9cd-5fbb8d497e7a'),

-- Coach Permissions
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', '3c9e9e34-7a7f-4e49-8f3d-2c7a1f3c6b22'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', '3c9e9e34-7a7f-4e49-8f3d-2c7a1f3c6b22'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', 'd8e0e377-5f70-4f5b-a9bc-68a7b91c5a8d'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', 'd8e0e377-5f70-4f5b-a9bc-68a7b91c5a8d'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', '7aeb7c60-844b-4a38-b1ae-55829b8e5f3a'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', '7aeb7c60-844b-4a38-b1ae-55829b8e5f3a'),
    ('d12f1605-c1d1-4a81-bd27-ca118b1b55a5', 'e3b4a1d2-f0fa-4d56-b349-8bb7b78f99ff'),
    ('263f820c-5f83-4228-97e1-30e52f51b4ca', 'e3b4a1d2-f0fa-4d56-b349-8bb7b78f99ff');

-- CREATE APPOINTMENT STATUSES
INSERT INTO AppointmentStatuses (id, name, description) VALUES
                                                            ('9f1a0b47-1f9d-4a6d-bcfc-5ed5e91cdaf7', 'Approved', 'Approved status.'),
                                                            ('4f82c9d9-3346-4c53-8d36-2f6e7186e1a3', 'Declined', 'Declined status.'),
                                                            ('6a79d224-c6a5-4f14-90ad-ec9e7a2743c2', 'InProgress', 'In Progress status.'),
                                                            ('27c2b8b8-d95d-4e35-8df6-90f8b31a22aa', 'Pending', 'Pending status.'),
                                                            ('e2031af4-e2d7-440d-a88b-b7e09fff9805', 'Canceled', 'Canceled status.');

-- CREATE APPOINTMENT REQUEST TYPES
INSERT INTO AppointmentRequestTypes (id, name, description) VALUES
                                                                ('15357c44-7a67-4bfa-bcf7-bbfb50d90a1b', 'CancelationRequest', 'cancelation-request'),
                                                                ('fe475b5b-1757-437a-9ade-3cab41aee797', 'JoinAppointmentRequest', 'join-appointment-request');

-- CREATE APPOINTMENT REQUEST STATUSES
INSERT INTO AppointmentRequestStatuses (id, name, description) VALUES
                                                                   ('b9ffb94b-b575-4f04-bac6-8d800aa84357', 'Approved', 'Approved request.'),
                                                                   ('8168dc3b-e88d-458c-b837-f6f36bd450e8', 'Declined', 'Declined request.'),
                                                                   ('56651beb-85fe-496a-9dc8-35860bd74be0', 'InProgress', 'Request is in progress.'),
                                                                   ('fb6de924-ba35-4972-8800-167e1f6a994d', 'Pending', 'Pending request.'),
                                                                   ('61ae9165-879e-45d6-b94a-19769a1adcc2', 'Canceled', 'Canceled request.');

-- CREATE APPOINTMENT TYPES
INSERT INTO AppointmentTypes (id, name, description) VALUES
                                                         ('7b8f6c62-8a91-4c7e-bb65-fb9ad7d0b55a', 'Legs', 'legs'),
                                                         ('4dce86e3-d24b-43cb-9f1f-fec3ed5bc8f9', 'Back', 'back'),
                                                         ('f3e37c12-d674-4f0d-9790-18c03c33f4a2', 'Arms', 'arms'),
                                                         ('d6a52901-d88e-4de3-a65c-47f1cd6231d6', 'Cardio', 'cardio'),
                                                         ('2fd98f7f-0bc1-4127-9ac5-538d89649c9e', 'Chest', 'chest'),
                                                         ('f2a3c1e0-47f1-4c8f-b9c6-8d3788c3e9df', 'Strength and Conditioning', 'strength-and-conditioning');

-- CREATE TIME SLOTS
INSERT INTO TimeSlots (Id, Day, StartTime, EndTime) VALUES
                                                        ('550c5105-ac78-42c9-9636-c799452c9b48', 1, '06:15:00', '07:15:00'),
                                                        ('e44579c3-eaea-492e-884f-94daf8288d5f', 2, '06:15:00', '07:15:00'),
                                                        ('013733e4-37e3-4224-a53b-63e15fca3fb5', 3, '06:15:00', '07:15:00'),
                                                        ('b5123094-efcc-4618-8a3e-0f93a1aed50c', 4, '06:15:00', '07:15:00'),
                                                        ('78a05c6b-965e-48b8-b8e2-fd481036fcc3', 5, '06:15:00', '07:15:00'),

                                                        ('d487a844-4081-4967-a801-aaea35fd7f00', 1, '07:15:00', '08:15:00'),
                                                        ('f8f4496f-bfa2-4638-8acc-319bc1bd56c3', 2, '07:15:00', '08:15:00'),
                                                        ('f71a5586-bd5b-4c7a-bfb9-1e813126f169', 3, '07:15:00', '08:15:00'),
                                                        ('095a78eb-2f35-4d2d-b788-5665533a7aa6', 4, '07:15:00', '08:15:00'),
                                                        ('5b5729cb-f25d-46ce-b1a2-e7a9f0d789b0', 5, '07:15:00', '08:15:00'),

                                                        ('5d259289-3392-47b6-b826-8e8abca59046', 1, '08:15:00', '09:15:00'),
                                                        ('3fdf842b-6e8d-4dc7-b9da-0fdb07a6f2b8', 2, '08:15:00', '09:15:00'),
                                                        ('bd8775e6-d604-4913-bdbc-084ccfbcc0ef', 3, '08:15:00', '09:15:00'),
                                                        ('c22ec84f-8720-463d-bb9a-9e8ec8728710', 4, '08:15:00', '09:15:00'),
                                                        ('f2be080f-c56c-4f2b-b0a7-b19ef84095c1', 5, '08:15:00', '09:15:00'),

                                                        ('4b92fcf1-a4f4-4c2e-bae4-73acfdfabd9c', 1, '09:15:00', '10:15:00'),
                                                        ('c441050a-7aa9-4d36-8326-bb0ec64f2e48', 2, '09:15:00', '10:15:00'),
                                                        ('e5f62a0b-9b2c-43da-8a23-410ffe9a8942', 3, '09:15:00', '10:15:00'),
                                                        ('3b7be5d1-fadd-43c5-9a2c-c9dd51d14420', 4, '09:15:00', '10:15:00'),
                                                        ('c60ad513-0b44-4934-9bd6-e73e27aee6c3', 5, '09:15:00', '10:15:00'),
                                                        ('814ce953-bfc0-4e33-989a-6480fe8f4cca', 6, '09:15:00', '10:15:00'),

                                                        ('e5183c12-f2d7-47c1-8ccb-4b2facd574f6', 1, '10:30:00', '11:30:00'),
                                                        ('28cda7be-6c2b-46bd-84fc-05bde8a2bc27', 2, '10:30:00', '11:30:00'),
                                                        ('3eb69908-0573-4931-ab6c-7ab427616b95', 3, '10:30:00', '11:30:00'),
                                                        ('0d075ef2-4fc1-4b3d-a7df-4f16f7fb2659', 4, '10:30:00', '11:30:00'),
                                                        ('0579c37d-7996-4afd-b5b2-8952c93db67d', 5, '10:30:00', '11:30:00'),
                                                        ('7de932ec-32a0-4315-85c7-f4b593aa1430', 6, '10:30:00', '11:30:00'),

                                                        ('a371efea-48cc-4746-b34b-2d1a5ce18699', 1, '11:30:00', '12:30:00'),
                                                        ('a69302e5-8a40-4edc-b797-40330d922391', 2, '11:30:00', '12:30:00'),
                                                        ('49915817-05cb-46cb-8200-d41dad5aad15', 3, '11:30:00', '12:30:00'),
                                                        ('c64e30c4-8eed-4a61-a8fe-7d32be86c251', 4, '11:30:00', '12:30:00'),
                                                        ('0950a6f8-fccb-4f3d-b0bf-e777ef91f3fe', 5, '11:30:00', '12:30:00'),
                                                        ('5f002353-b8b2-4c27-bed6-74b8f4db127b', 6, '11:30:00', '12:30:00'),

                                                        ('2e20406c-a2d7-406a-9845-94f745358eaf', 1, '12:30:00', '13:30:00'),
                                                        ('127989db-bbfb-40f2-a116-8cc066afc6ca', 2, '12:30:00', '13:30:00'),
                                                        ('24e4cc1a-933b-44fb-961d-09fe6a88df03', 3, '12:30:00', '13:30:00'),
                                                        ('28fd551e-9244-4fcc-8524-39c1a6edbffc', 4, '12:30:00', '13:30:00'),
                                                        ('a0cb8262-f8a1-4ac8-a170-bc54fccda4ae', 5, '12:30:00', '13:30:00'),
                                                        ('62d9282e-e779-4c20-8ed5-7d36e09b4227', 6, '12:30:00', '13:30:00'),

                                                        ('de6e0ca5-6bba-43c1-9f9e-8a78f263d7c1', 1, '13:30:00', '14:30:00'),
                                                        ('3e8a7555-a2f0-4000-9a48-24fcfb4200e9', 2, '13:30:00', '14:30:00'),
                                                        ('89016146-50b2-4b3d-95db-12d340c2631c', 3, '13:30:00', '14:30:00'),
                                                        ('3166a419-1936-4467-8374-62bc264e1970', 4, '13:30:00', '14:30:00'),
                                                        ('b4f603bd-c151-45c9-8128-1ef241e8e8ea', 5, '13:30:00', '14:30:00'),
                                                        ('8fd5d22a-c535-4fd1-8260-92976faa36c8', 6, '13:30:00', '14:30:00');


-- LINK ALL TIMESLOTS TO COACH
INSERT INTO CoachTimeSlots (CoachId, TimeSlotId) VALUES
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '550c5105-ac78-42c9-9636-c799452c9b48'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'e44579c3-eaea-492e-884f-94daf8288d5f'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '013733e4-37e3-4224-a53b-63e15fca3fb5'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'b5123094-efcc-4618-8a3e-0f93a1aed50c'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '78a05c6b-965e-48b8-b8e2-fd481036fcc3'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'd487a844-4081-4967-a801-aaea35fd7f00'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'f8f4496f-bfa2-4638-8acc-319bc1bd56c3'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'f71a5586-bd5b-4c7a-bfb9-1e813126f169'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '095a78eb-2f35-4d2d-b788-5665533a7aa6'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '5b5729cb-f25d-46ce-b1a2-e7a9f0d789b0'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '5d259289-3392-47b6-b826-8e8abca59046'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '3fdf842b-6e8d-4dc7-b9da-0fdb07a6f2b8'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'bd8775e6-d604-4913-bdbc-084ccfbcc0ef'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'c22ec84f-8720-463d-bb9a-9e8ec8728710'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'f2be080f-c56c-4f2b-b0a7-b19ef84095c1'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '4b92fcf1-a4f4-4c2e-bae4-73acfdfabd9c'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'c441050a-7aa9-4d36-8326-bb0ec64f2e48'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'e5f62a0b-9b2c-43da-8a23-410ffe9a8942'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '3b7be5d1-fadd-43c5-9a2c-c9dd51d14420'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'c60ad513-0b44-4934-9bd6-e73e27aee6c3'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '814ce953-bfc0-4e33-989a-6480fe8f4cca'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'e5183c12-f2d7-47c1-8ccb-4b2facd574f6'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '28cda7be-6c2b-46bd-84fc-05bde8a2bc27'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '3eb69908-0573-4931-ab6c-7ab427616b95'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '0d075ef2-4fc1-4b3d-a7df-4f16f7fb2659'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '0579c37d-7996-4afd-b5b2-8952c93db67d'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '7de932ec-32a0-4315-85c7-f4b593aa1430'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'a371efea-48cc-4746-b34b-2d1a5ce18699'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'a69302e5-8a40-4edc-b797-40330d922391'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '49915817-05cb-46cb-8200-d41dad5aad15'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'c64e30c4-8eed-4a61-a8fe-7d32be86c251'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '0950a6f8-fccb-4f3d-b0bf-e777ef91f3fe'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '5f002353-b8b2-4c27-bed6-74b8f4db127b'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '2e20406c-a2d7-406a-9845-94f745358eaf'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '127989db-bbfb-40f2-a116-8cc066afc6ca'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '24e4cc1a-933b-44fb-961d-09fe6a88df03'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '28fd551e-9244-4fcc-8524-39c1a6edbffc'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'a0cb8262-f8a1-4ac8-a170-bc54fccda4ae'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '62d9282e-e779-4c20-8ed5-7d36e09b4227'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'de6e0ca5-6bba-43c1-9f9e-8a78f263d7c1'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '3e8a7555-a2f0-4000-9a48-24fcfb4200e9'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '89016146-50b2-4b3d-95db-12d340c2631c'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '3166a419-1936-4467-8374-62bc264e1970'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', 'b4f603bd-c151-45c9-8128-1ef241e8e8ea'),
                                                     ('8c02324d-8150-40ee-752b-08ddb5679385', '8fd5d22a-c535-4fd1-8260-92976faa36c8');