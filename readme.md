# OOOControlSystem - Defect management system at construction sites

[![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Entity Framework](https://img.shields.io/badge/Entity%20Framework-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/ef/)
[![JWT](https://img.shields.io/badge/JWT-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)](https://jwt.io/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![REST API](https://img.shields.io/badge/REST%20API-FF6C37?style=for-the-badge&logo=rest&logoColor=white)](https://restfulapi.net/)

## 📋 О проекте

OOOControlSystem - это монолитное веб-приложение для централизованного управления дефектами на строительных объектах. Система обеспечивает полный цикл работы: от регистрации дефекта и назначения исполнителя до контроля статусов и формирования отчётности для руководства.

### 👥 Целевая аудитория

- **Инженеры** - регистрация дефектов, обновление информации
- **Менеджеры** - назначение задач, контроль сроков, формирование отчетов
- **Руководители и заказчики** - просмотр прогресса и аналитической отчетности

## 🏗️ Архитектурный обзор

Проект реализован как монолитное веб-приложение с использованием .NET и Entity Framework. Архитектура разделена на следующие слои:

### Структура проекта

OOOControlSystem/
├── Controllers/           # MVC Контроллеры
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── ProjectsController.cs
│   └── DefectsController.cs
├── Models/               # Модели данных
│   ├── User.cs
│   ├── Project.cs
│   ├── Defect.cs
│   └── Enums/
├── Services/             # Бизнес-логика
│   ├── AuthService.cs
│   └── TokenService.cs
├── Dtos/                 # Data Transfer Objects
├── Middleware/           # Промежуточное ПО
│   └── TokenValidationMiddleware.cs
├── ApplicationContext.cs # Контекст БД
├── appsettings.json      # Конфигурация подключений
└── Program.cs           # Конфигурация приложения

### Модели данных (`Models/`)
- `User` - пользователи системы с ролевой моделью
- `Project` - строительные проекты/объекты
- `Defect` - дефекты с историей изменений и вложениями

### Модели (Model Layer)
```mermaid
classDiagram
    class User {
        +int Id
        +string Email
        +string PasswordHash
        +string FullName
        +UserRole Role
        +List~Project~ CreatedProjects
        +List~Defect~ ReportedDefects
    }

    class Project {
        +int Id
        +string Name
        +string Description
        +ProjectStatus Status
        +User Creator
        +User Owner
        +List~Defect~ Defects
    }

    class Defect {
        +int Id
        +string Title
        +string Description
        +DefectStatus Status
        +DefectPriority Priority
        +User Reporter
        +User AssignedUser
        +Project Project
        +List~DefectHistoryEntry~ History
    }

    User "1" -- "*" Project : creates
    User "1" -- "*" Project : owns
    User "1" -- "*" Defect : reports
    User "1" -- "*" Defect : assigned_to
    Project "1" -- "*" Defect : contains
```

### Сервисы (`Services/`)
- `AuthService` - аутентификация и регистрация пользователей
- `TokenService` - генерация и валидация JWT-токенов

### Контроллеры (`Controllers/`)
- `AuthController` - endpoints для аутентификации
- `UsersController` - управление пользователями
- `ProjectsController` - управление проектами
- `DefectsController` - управление дефектами

### Middleware
- `TokenValidationMiddleware` - проверка валидности токенов через TokenVersion

## 🔐 Безопасность

- **Аутентификация** - JWT tokens с версионированием
- **Хранение паролей** - bcrypt хеширование
- **Ролевая модель** - строгое разграничение прав доступа

## 🛠️ Технологический стек

- **Backend**: .NET 8, Entity Framework Core, ASP.NET
- **База данных**: PostgreSQL
- **Аутентификация**: JWT Bearer
- **Безопасность**: bcrypt