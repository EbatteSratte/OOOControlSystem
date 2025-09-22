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

```bash
OOOControlSystem/
├── 📂 Controllers/                 # MVC Контроллеры (API endpoints)
│   ├── 🔷 AuthController.cs        # Аутентификация и регистрация
│   ├── 🔷 UsersController.cs       # Управление пользователями
│   ├── 🔷 ProjectsController.cs    # Управление проектами
│   └── 🔷 DefectsController.cs     # Управление дефектами
│
├── 📂 Models/                      # Модели данных (Entity Framework)
│   ├── 🔷 User.cs                  # Сущность пользователя
│   ├── 🔷 Project.cs               # Сущность проекта
│   ├── 🔷 Defect.cs                # Сущность дефекта
│   └── 📂 Enums/                   # Перечисления
│       ├── 🔷 UserRole.cs          # Роли пользователей
│       ├── 🔷 ProjectStatus.cs     # Статусы проектов
│       ├── 🔷 DefectStatus.cs      # Статусы дефектов
│       └── 🔷 DefectPriority.cs    # Приоритеты дефектов
│
├── 📂 Services/                    # Бизнес-логика и сервисы
│   ├── 🔷 AuthService.cs           # Сервис аутентификации
│   └── 🔷 TokenService.cs          # Сервис работы с JWT-токенами
│
├── 📂 Dtos/                        # Data Transfer Objects
│   ├── 🔷 LoginDto.cs              # DTO для входа
│   ├── 🔷 RegisterDto.cs           # DTO для регистрации
│   ├── 🔷 UserUpdateDto.cs         # DTO для обновления пользователя
│   ├── 🔷 ProjectCreateDto.cs      # DTO для создания проекта
│   └── 🔷 ProjectUpdateDto.cs      # DTO для обновления проекта
│
├── 📂 Middleware/                  # Промежуточное ПО
│   └── 🔷 TokenValidationMiddleware.cs # Валидация JWT-токенов
│
├── 🔷 ApplicationContext.cs    # Контекст базы данных EF Core
│
├── 📜 Program.cs                   # Конфигурация приложения и DI
├── 📜 appsettings.json             # Конфигурационные параметры
├── 📜 OOOControlSystem.csproj      # Файл проекта
└── 📜 README.md                    # Документация проекта
```

### 📊 ER-диаграмма базы данных
```mermaid
erDiagram
    USER {
        int id PK "Идентификатор"
        string email "Email"
        string password_hash "Хеш пароля"
        string full_name "Полное имя"
        string role "Роль"
        bool is_active "Активен"
        int token_version "Версия токена"
        datetime created_at "Дата создания"
    }

    PROJECT {
        int id PK "Идентификатор"
        string name "Название"
        string description "Описание"
        string status "Статус"
        int created_by_id FK "Создатель"
        int owner_id FK "Владелец"
        datetime created_at "Дата создания"
    }

    DEFECT {
        int id PK "Идентификатор"
        string title "Заголовок"
        string description "Описание"
        string status "Статус"
        string priority "Приоритет"
        int project_id FK "Проект"
        int assigned_to_id FK "Исполнитель"
        int reported_by_id FK "Автор"
        datetime due_date "Срок исправления"
        datetime created_at "Дата создания"
        datetime updated_at "Дата обновления"
        jsonb attachment_paths "Вложения"
        jsonb history "История изменений"
    }

    USER ||--o{ PROJECT : "создает"
    USER ||--o{ PROJECT : "владеет"
    USER ||--o{ DEFECT : "сообщает"
    USER ||--o{ DEFECT : "назначен"
    PROJECT ||--o{ DEFECT : "содержит"
```

## 🔐 Безопасность

- **Аутентификация** - JWT tokens с версионированием
- **Хранение паролей** - bcrypt хеширование
- **Ролевая модель** - строгое разграничение прав доступа

## 🛠️ Технологический стек

- **Backend**: .NET 8, Entity Framework Core, ASP.NET
- **База данных**: PostgreSQL
- **Аутентификация**: JWT Bearer
- **Безопасность**: bcrypt
