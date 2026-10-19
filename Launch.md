# Запуск

Нужны .NET SDK 10 и, для живого диалога, Ollama. Тесты SDK запускает без модели.

## 1. Модель

```bash
ollama serve
ollama pull qwen3.5:4b
```

Адрес по умолчанию: `http://localhost:11434`. Имя модели по умолчанию: `qwen3.5:4b`. Ключ API не нужен.

Другая модель или другой компьютер:

```bash
cp .env.example .env
```

В `.env` меняются `OLLAMA_BASE_URL`, `OLLAMA_MODEL`, `PERSON_API_BASE_URL`. Файл `.env` не коммитится.

## 2. API

```bash
dotnet run --project src/PersonApi
```

Слушает `http://localhost:5080`. Swagger: http://localhost:5080/swagger.

Операции: `POST /persons`, `GET /persons/{id}`, `PUT /persons/{id}`, `GET /persons`. Успех — `201` или `200`. Ошибки тела — `400`, неизвестный id — `404`. Хранилище в памяти этого процесса.

## 3. Консоль

Во втором терминале:

```bash
dotnet run --project src/PersonAgent.Console -- "Создай физлицо: фамилия Иванов, имя Иван"
```

Без аргумента приложение само попросит текст. Пустая строка, текст короче 3 символов и текст длиннее 500 символов до модели не доходят. Для короткого и пустого ввода сообщение: `Укажите текст от 3 до 500 символов`.

Ответ всегда из четырёх строк:

```text
Status: success | error
Action: CreatePerson
Data: Готово. Физлицо создано. Идентификатор: 1.
Errors: нет
```

После изменения данных в результате есть фраза `Готово. Данные физлица изменены`. Сырой JSON человеку не печатается. Логи tool и HTTP — в stderr.

Проверка без модели: фраза длиннее 500 символов должна завершиться кодом `1` и текстом `длина текста превышает 500 символов`, без обращения к Ollama.

## 4. Тесты

```bash
dotnet test
```

Живой прогон агента против OpenAI-совместимого endpoint, если он есть:

```bash
export LLM_INTEGRATION_BASE_URL=http://localhost:11434/v1
export LLM_INTEGRATION_MODEL=qwen3.5:4b
export LLM_INTEGRATION_API_KEY=ollama
dotnet test --filter LiveModel_CreatePerson_WhenConfigured
```

Без этих переменных тест пропускается.

## Если не стартует

- `Ollama недоступен` — не запущен `ollama serve` или неверный `OLLAMA_BASE_URL`.
- `API недоступен` — не запущен проект `src/PersonApi` или порт не `5080`.
- Модель отвечает бессмыслицей — в `OLLAMA_MODEL` поставьте инструкционную модель, которая держит формат Action / Action Input. Для проверки кода модель не нужна: `dotnet test`.
- Данные пропали — API перезапускали. Память не сохраняется на диск.
