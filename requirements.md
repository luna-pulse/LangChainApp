# Зависимости

В задании преподавателя зависимости предлагается зафиксировать в `requirements.txt` или `pyproject.toml`. Проект по ТЗ автора написан на C# / .NET, поэтому тот же смысл перенесён в средства NuGet.

| Python | .NET в этом репозитории |
| --- | --- |
| `requirements.txt` со списком пакетов и версий | `Directory.Packages.props`: центральные версии прямых пакетов |
| lock-файл (`poetry.lock`, `uv.lock`) | `packages.lock.json` рядом с каждым проектом |
| `pip install -r requirements.txt` | `dotnet restore` |
| `python main.py "..."` | `dotnet run --project src/PersonAgent.Console -- "..."` |
| виртуальное окружение и интерпретатор | .NET SDK 10, `global.json` |

Прямые пакеты и зачем они нужны:

| Пакет | Версия | Роль |
| --- | --- | --- |
| `LangChain` | 0.17.1 | Агент ReAct, `AgentTool`, цепочки. Официальная C#-реализация LangChain (tryAGI). |
| `Microsoft.Extensions.AI` | 10.4.1 | `IChatClient`, который LangChain 0.17.1 принимает как модель. |
| `Swashbuckle.AspNetCore` | 10.3.0 | Swagger UI для Minimal API. |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.13 | Тестовый сервер API. |
| `xunit` | 2.9.3 | Тесты. |
| `Xunit.SkippableFact` | 1.5.23 | Пропуск живого LLM-теста, если endpoint не задан. |

Пакет `Ollama` 1.15, который подтягивает сам LangChain, на момент версии 0.17.1 не реализует `IChatClient` (это прямо отмечено в тесте репозитория LangChain `WikiTests.AgentWithOllama`). Поэтому чат с локальной Ollama идёт через тонкий `OllamaChatClient` в `src/PersonAgent.Core/Llm/OllamaChatClient.cs`: он реализует тот же интерфейс, который ждёт `ReActAgentExecutor`, и вызывает нативный `POST /api/chat`. Модель по умолчанию — `qwen3.5:4b` (`Ollama:Model`, `OLLAMA_MODEL`). Агентный цикл, tools и разбор Action / Action Input остаются кодом LangChain, а не другим фреймворком.
