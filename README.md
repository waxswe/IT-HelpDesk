# IT-HelpDesk

Система управления заявками в службу поддержки. WPF + Entity Framework (Database First) + SQL Server.

## Быстрый запуск

### 1. Скачайте репозиторий

`git clone https://github.com/waxswe/IT-HelpDesk.git`

Или скачайте ZIP‑архив.

### 2. Откройте решение

Файл `IT-HelpDesk.sln` (Visual Studio 2019/2022).

### 3. Создайте базу данных

Выполните скрипт `СозданиеБД.sql` на вашем SQL Server. Будет создана БД `ITHelpDesk`.

### 4. Настройте подключение (Entity Framework)

1. В папке `Data` удалите `.edmx` (если есть).
2. В `App.config` удалите строку подключения `ITHelpDeskEntities`.
3. ПКМ по `Data` → Add → New Item → ADO.NET Entity Data Model → имя `ITHelpDeskEntities` → Add.
4. Выберите `Конструктор EF из базы данных` → Next.
5. New Connection → укажите имя сервера (например, `DESKTOP-ABC123\SQLEXPRESS`) → выберите аутентификацию.
6. Поставьте галочку `Доверять сертификату сервера`.
7. Выберите базу `ITHelpDesk` → OK → Next.
8. Оставьте имя `ITHelpDeskEntities` → Next.
9. Выберите все таблицы.
10. Поставьте галочку `Формировать имена объектов во множественном или единственном числе`.
11. Finish.

### 5. Запустите

Нажмите `F5` в Visual Studio.  
Логин: `admin`  
Пароль: `1`

### 6. Как найди .exe

Зайдите в корневую папку IT-HelpDesk → bin → Debug → IT-HelpDesk.exe

`\IT-HelpDesk\IT-HelpDesk\bin\Debug\IT-HelpDesk.exe`
