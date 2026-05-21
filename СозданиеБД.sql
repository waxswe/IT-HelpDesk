CREATE DATABASE ITHelpDesk;
GO

USE ITHelpDesk;
GO

-- Справочник ролей
CREATE TABLE Roles (
    roleID INT IDENTITY(1,1) PRIMARY KEY,
    role NVARCHAR(50) NOT NULL UNIQUE
);

-- Справочник статусов пользователей
CREATE TABLE Statuses (
    statusID INT IDENTITY(1,1) PRIMARY KEY,
    status NVARCHAR(50) NOT NULL UNIQUE
);

-- Справочник профессий исполнителей
CREATE TABLE Professions (
    professionID INT IDENTITY(1,1) PRIMARY KEY,
    profession NVARCHAR(50) NOT NULL UNIQUE
);

-- Таблица пользователей
CREATE TABLE Users (
    userID INT IDENTITY(1,1) PRIMARY KEY,
    login NVARCHAR(16) NOT NULL UNIQUE,
	password NVARCHAR(255) NOT NULL,
	name NVARCHAR(64) NOT NULL,
	avatarURL NVARCHAR(255) NOT NULL DEFAULT '/Data/Images/avatar.jpg',
	roleID INT FOREIGN KEY REFERENCES Roles(roleID) NOT NULL,
	statusID INT FOREIGN KEY REFERENCES Statuses(statusID) NOT NULL,
	professionID INT FOREIGN KEY REFERENCES Professions(professionID) DEFAULT NULL,
	createdAt DATETIME DEFAULT GETDATE(),
	email NVARCHAR(64),
	phone NVARCHAR(16),
	mistakeCount INT CHECK (mistakeCount >= 0) DEFAULT 0,
	plainPassword NVARCHAR(255),
	isNew BIT DEFAULT 0
);

-- Справочник разделов заявок
CREATE TABLE RequestSections (
    requestSectionID INT IDENTITY(1,1) PRIMARY KEY,
    requestSection NVARCHAR(128) NOT NULL UNIQUE
);

-- Справочник категорий заявок
CREATE TABLE RequestCategories (
    requestCategoryID INT IDENTITY(1,1) PRIMARY KEY,
	requestSectionID INT FOREIGN KEY REFERENCES RequestSections(requestSectionID) NOT NULL,
    requestCategory NVARCHAR(128) NOT NULL UNIQUE
);

-- Справочник статусов обращений
CREATE TABLE RequestStatuses (
    requestStatusID INT IDENTITY(1,1) PRIMARY KEY,
    requestStatus NVARCHAR(50) NOT NULL UNIQUE
);

-- Таблица заявок
CREATE TABLE Requests (
    requestID INT IDENTITY(1,1) PRIMARY KEY,
    clientID INT FOREIGN KEY REFERENCES Users(userID) NOT NULL,
    title NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX) DEFAULT 'Описание отсутствует',
    requestStatusID INT FOREIGN KEY REFERENCES RequestStatuses(requestStatusID) NOT NULL,
    requestCategoryID INT FOREIGN KEY REFERENCES RequestCategories(requestCategoryID) NOT NULL,
    workerID INT FOREIGN KEY REFERENCES Users(userID),
    createdAt DATETIME DEFAULT GETDATE(),
    updatedAt DATETIME,
    updatedBy INT FOREIGN KEY REFERENCES Users(userID)
);

-- Справочник событий комментариев
CREATE TABLE CommentEvents (
    eventID INT IDENTITY(1,1) PRIMARY KEY,
    eventType NVARCHAR(128) NOT NULL
);

-- Таблица комментариев
CREATE TABLE Comments (
    commentID INT IDENTITY(1,1) PRIMARY KEY,
    requestID INT NOT NULL FOREIGN KEY REFERENCES Requests(requestID),
    userID INT NULL FOREIGN KEY REFERENCES Users(userID),
    isSystem BIT NOT NULL DEFAULT 0,
    eventID INT NULL FOREIGN KEY REFERENCES CommentEvents(eventID),
    text NVARCHAR(MAX) NOT NULL,
    createdAt DATETIME DEFAULT GETDATE(),
    isEdited BIT NOT NULL DEFAULT 0,
	updatedAt DATETIME
);

-- Справочник статусов уведомлений
CREATE TABLE NotificationStatuses (
    notificationStatusID INT IDENTITY(1,1) PRIMARY KEY,
    notificationStatus NVARCHAR(50) NOT NULL
);

-- Справочник шаблонов уведомлений
CREATE TABLE NotificationTemplates (
    templateID INT IDENTITY(1,1) PRIMARY KEY,
	templateKey NVARCHAR(128) NOT NULL,
    template NVARCHAR(MAX) NOT NULL
);

-- Таблица уведомлений
CREATE TABLE Notifications (
    notificationID INT IDENTITY(1,1) PRIMARY KEY,
    userID INT NOT NULL FOREIGN KEY REFERENCES Users(userID),
    notificationStatusID INT NOT NULL FOREIGN KEY REFERENCES NotificationStatuses(notificationStatusID),
    templateID INT NOT NULL FOREIGN KEY REFERENCES NotificationTemplates(templateID),
    initiatorID INT NULL FOREIGN KEY REFERENCES Users(userID),
    requestID INT NULL  FOREIGN KEY REFERENCES Requests(requestID),
    createdAt DATETIME DEFAULT GETDATE(),
	message NVARCHAR(MAX) DEFAULT '',
	isRead BIT NOT NULL DEFAULT 0
);

--- Запросы
-- Справочник ролей
 INSERT INTO Roles (role) VALUES
	('Администратор'),
	('Пользователь'),
	('Менеджер'),
	('Исполнитель');

-- Справочник статусов
INSERT INTO Statuses (status) VALUES
	('Разблокирован'),
	('Заблокирован'),
	('Удалён');

-- Справочник профессий
INSERT INTO Professions (profession) VALUES
    ('Сантехник'),
    ('Электрик'),
    ('Уборщик'),
    ('Слесарь'),
    ('Системный администратор'),
    ('Специалист по ремонту ПК'),
    ('Сетевой инженер'),
    ('Специалист по программному обеспечению');

-- Пользователь (администратор)
INSERT INTO Users (login, password, name, roleID, statusID, email, plainPassword) VALUES
	('admin', 'a4ayc/80/OGda4BO/1o/V0etpOqiLx1JwB5S3beHW0s=', 'Администратор', 1, 1, 'administrator@it.nn', 1);

-- Справочник разделов категорий заявок
INSERT INTO RequestSections (requestSection) VALUES
	('Административно-хозяйственный'),
	('Компьютерная техника и ПО'),
	('Другое');

-- Справочник категорий заявок
INSERT INTO RequestCategories (requestSectionID, requestCategory) VALUES
    (1, 'Ремонт помещения'),
	(1, 'Техническое обслуживание'),
    (1, 'Уборка и клининг'),
    (1, 'Обслуживание помещений'),
    (1, 'Доступ и пропуски'),
    (2, 'Сбой программного обеспечения'),
    (2, 'Ошибка в работе ПК'),
    (2, 'Сетевое оборудование'),
    (2, 'Техническая поддержка'),
	(2, 'Доступ, учётные записи и информационная безопасность'),
	(2, 'Периферийное оборудование и печать'),
    (3, 'Прочее');

-- Справочник статусов заявок
INSERT INTO RequestStatuses (requestStatus) VALUES
	('Новая'),
	('Назначена'),
	('В работе'),
	('Ожидает ответа клиента'),
	('Выполнена'),
	('Отменена'),
	('Закрыта');

-- Статусы уведомлений
INSERT INTO NotificationStatuses (notificationStatus) VALUES 
	('Новое'), 
	('Прочитано');

-- Шаблоны уведомлений (templateKey, template на русском)
INSERT INTO NotificationTemplates (templateKey, template) VALUES
	('Notification_NewRequest_ToManager', 'Создана новая заявка #{0}'),
	('Notification_Assigned_ToExecutor', 'Вам назначена заявка #{0}'),
	('Notification_Assigned_ToClient', 'В заявке #{0} назначен исполнитель {1}'),
	('Notification_Comment_ToClient', 'В вашей заявке #{0} оставлен комментарий'),
	('Notification_Comment_ToExecutor', 'В заявке #{0}, где вы участвуете, оставлен комментарий'),
	('Notification_Completed_ToManager', 'Заявка #{0} выполнена исполнителем {1}'),
	('Notification_Completed_ToClient', 'Ваша заявка #{0} выполнена'),
	('Notification_Closed_ToManager', 'Клиент закрыл заявку #{0}'),
	('Notification_UserBlocked_ToAdmin', 'Пользователь {0} был заблокирован из-за превышения числа попыток входа'),
	('Notification_StatusChanged', 'Статус заявки #{0} был изменён на ''{1}''.'),
	('Notification_ExecutorRemoved', 'Ваше назначение в заявке #{0} отменено'),
	('Notification_NeedManager_ToManager', 'В заявке #{0} требуется вмешательство менеджера'),
	('MassMailing_Notification', 'Массовая рассылка: {0}'),
	('Success_Request_Created', 'Заявка #{0} успешно создана!'),
	('Success_Request_Cancelled', 'Заявка #{0} успешно отменена!'),
	('Success_Request_Closed', 'Заявка #{0} успешно закрыта!'),
	('Success_Request_Completed', 'Заявка #{0} успешно завершена!'),
	('Notification_UserDataChanged', 'Администратор {0} изменил ваши данные'),
	('Notification_AvatarDeleted_ToUser', 'Администратор {0} удалил ваш аватар');

-- События комментариев
INSERT INTO CommentEvents (eventType) VALUES 
	('Created'),
	('Assigned'),
	('StatusChanged'),
	('Completed'),
	('ExecutorRemoved');

CREATE INDEX IX_Notifications_userID_isRead ON Notifications(userID, isRead);
CREATE INDEX IX_Notifications_createdAt ON Notifications(createdAt DESC);