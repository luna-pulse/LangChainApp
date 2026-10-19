# Как работать в этом репозитории

Проект на C# и .NET 10. Python-код и смена агентного фреймворка не подходят: агент собран на LangChain .NET (`Chain.ReActAgentExecutor`, `AgentTool`).

## Структура

- `src/PersonApi` — Minimal API физлиц, Swagger, память процесса.
- `src/PersonAgent.Core` — агент, tools, HTTP-клиент, формат ответа, клиенты Ollama и OpenAI-compatible.
- `src/PersonAgent.Console` — точка входа. Читает `.env`, `appsettings.json` и переменные окружения.
- `tests/PersonApi.Tests`, `tests/PersonAgent.Tests` — xUnit.
- `Prompts/system.prompt` — единственный рабочий промпт агента. Его подхватывает консоль и тесты.
- `Directory.Packages.props` и `packages.lock.json` — зафиксированные версии. Версию пакета меняют там, а не только в `.csproj`.

## Команды

```bash
dotnet build
dotnet test
dotnet run --project src/PersonApi
dotnet run --project src/PersonAgent.Console -- "Создай физлицо: фамилия Иванов, имя Иван"
```

`dotnet test` не требует Ollama. Живой LLM-тест включается только переменными `LLM_INTEGRATION_BASE_URL` и `LLM_INTEGRATION_MODEL`.

CI — `.github/workflows/ci.yml`. Он запускается на каждый push и на pull request: SDK из `global.json`, `dotnet restore PersonAgent.slnx --locked-mode`, `dotnet build`, `dotnet test`. Opt-in тест живой модели в CI пропускается. Локально ту же проверку зависимостей даёт `dotnet restore --locked-mode`.

## Контракт, который нельзя размывать

Четыре tool и только они: `CreatePerson`, `GetPerson`, `UpdatePerson`, `ListPersons`.

Каждый tool валидирует JSON-аргументы и вызывает свой метод API. Имя, URL и путь из текста модели не исполняются. В логе контракт выглядит так: `{"name":"CreatePerson","args":{...}}`. Фамилия и имя в логе — `***`.

Ответ человеку — ровно четыре строки `Status` / `Action` / `Data` / `Errors`. Метки английские, текст внутри полей русский. Успех печатается только после успешного tool. Успешное изменение содержит `Готово. Данные физлица изменены`.

Ввод длиннее 500 символов и пустой ввод отсекаются до модели.

Имя tool внутри `ReActAgentExecutor` хранится в нижнем регистре: библиотека ищет его через `ToLower`. В контракте и в ответе имя остаётся PascalCase.

## Конфигурация

Секретов в репозитории нет. Ollama работает без ключа. Не коммитить `.env`. Шаблон — `.env.example`.

Адрес Ollama — корень сервера (`http://localhost:11434`), не `/api/chat`. Адрес API — `http://localhost:5080`.

## Стиль

Тексты для человека, промпт, README и отчёт — на русском. Метки итогового ответа фиксированы: `Status`, `Action`, `Data`, `Errors`. Имена типов и HTTP-полей — на английском, JSON в camelCase. Предупреждения компилятора считаются ошибками.
