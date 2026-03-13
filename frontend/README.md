# Support Chatbot — Frontend

Angular 18 standalone application that renders an embeddable chatbot widget. Communicates with the ASP.NET Core backend via a single `POST /api/chat` endpoint.

## Stack

- **Angular 18** (standalone components, no NgModules)
- **Tailwind CSS** (utility-first styling)
- **Angular Animations** (slide-in/out, button scale)
- **RxJS** (HTTP, reactive message stream)

## Structure

```
src/
├── app/
│   ├── app.component.html         # Host page — drop <app-chatbot-widget> anywhere
│   └── components/
│       └── chatbot-widget/        # Self-contained chat UI (open/close, messages, input)
├── services/
│   ├── chat.service.ts            # Sends messages, maps API responses to Message objects
│   └── app-config.service.ts      # Loads config.json before app bootstrap
├── models/
│   └── chat.models.ts             # Message, ChatResponse interfaces
└── assets/
    └── config.json                # Runtime config — set apiUrl here
```

## Configuration

**`src/assets/config.json`** — loaded at startup, no rebuild required when changing environments:

```json
{
  "apiUrl": "https://localhost:7275"
}
```

## Development

```bash
npm install
ng serve
```

Open `http://localhost:4200`. The widget appears fixed in the bottom-right corner.

## Production Build

```bash
ng build
```

Output lands in `dist/support-chatbot/`. Serve with any static host (Nginx, Azure Static Web Apps, GitHub Pages, Vercel, etc.).

## Embedding the Widget

The chatbot widget is a standalone Angular component. To embed it in an existing Angular app:

1. Copy the `chatbot-widget` component and its dependencies into your project.
2. Import `ChatbotWidgetComponent` in your host component.
3. Add `<app-chatbot-widget></app-chatbot-widget>` to your template.
4. Ensure `config.json` is served from your `assets/` folder.

## Customization

| What | Where |
|---|---|
| Bot name in header | `chatbot-widget.component.html` — `<h3>Support Assistant</h3>` |
| Initial greeting message | `chat.service.ts` — `getInitialMessage()` |
| Color scheme | Tailwind classes on the header div (`bg-blue-600`) and button |
| Widget position | `chatbot-widget.component.html` — `fixed bottom-6 right-6` |
| API header identifier | `chat.service.ts` — `'X-Business': 'your-business'` |
