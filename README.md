GNU GPL v3 license — you can do anything you want, but you must give me credit and distribute your changes under the same terms.

OutfitTool is a modular WPF application framework that serves as the core host for various utility modules. The application provides a centralized interface for managing modules, hotkeys, updates, and system integration.

Key features:

- **Modular architecture**: Dynamic loading of modules from the /modules directory with support for multiple API versions through adapter pattern
- **Module management**: Enables/disables modules, initializes controllers, and propagates language settings across all active modules
- **Hotkey system**: Global hotkey registration via Windows API (RegisterHotKey) with modifier key support (Ctrl, Alt, Shift) and persistent storage
- **System integration**: Taskbar icon with dynamic tooltip, balloon notifications, system tray context menu, and startup automation
- **Update manager**: Repository-based module updates with YAML manifest parsing, ZIP package downloading, extraction, and application restart
- **Settings persistence**: YAML-based configuration storage for module states, hotkeys, language, and UI preferences

The application uses a service locator pattern (ServiceLocator) to manage dependencies including ILogger, IModuleManager, HotKeyManager, and UpdatesManager. The ModuleManager loads assemblies dynamically and uses adapter classes (ModuleInfoAdapter, ModuleControllerAdapter) to bridge different API versions. The UI is built with WPF and includes a main window with tabbed interface for modules, hotkeys, logs, and updates. System integration is handled through WindowInteropHelper for message hooking and Hardcodet.Wpf.TaskbarNotification for tray functionality.

---

Лицензия GNU GPL v3 — вы можете делать с этим что угодно, но должны указать моё авторство и распространять свои изменения на тех же условиях.

OutfitTool — это модульный фреймворк для приложений WPF, который служит основным хостом для различных утилитарных модулей. Приложение предоставляет централизованный интерфейс для управления модулями, горячими клавишами, обновлениями и интеграцией с системой.

Ключевые особенности:

- **Модульная архитектура**: Динамическая загрузка модулей из директории /modules с поддержкой нескольких версий API через паттерн адаптера
- **Управление модулями**: Включение/отключение модулей, инициализация контроллеров и распространение настроек языка на все активные модули
- **Система горячих клавиш**: Регистрация глобальных горячих клавиш через Windows API (RegisterHotKey) с поддержкой модификаторов (Ctrl, Alt, Shift) и постоянным хранением
- **Интеграция с системой**: Иконка в панели задач с динамической подсказкой, всплывающие уведомления, контекстное меню в системном трее и автоматический запуск
- **Менеджер обновлений**: Обновление модулей из репозитория с парсингом YAML-манифеста, скачиванием ZIP-пакетов, распаковкой и перезапуском приложения
- **Сохранение настроек**: Хранение конфигурации на основе YAML для состояния модулей, горячих клавиш, языка и предпочтений интерфейса

Приложение использует паттерн Service Locator (ServiceLocator) для управления зависимостями, включая ILogger, IModuleManager, HotKeyManager и UpdatesManager. ModuleManager динамически загружает сборки и использует классы-адаптеры (ModuleInfoAdapter, ModuleControllerAdapter) для совмещения разных версий API. Интерфейс построен на WPF и включает главное окно с вкладками для модулей, горячих клавиш, логов и обновлений. Интеграция с системой осуществляется через WindowInteropHelper для перехвата сообщений и Hardcodet.Wpf.TaskbarNotification для функциональности иконки в трее.