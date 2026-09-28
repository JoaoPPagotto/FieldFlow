# Modelo de domínio

## Agregados

| Agregado | Responsabilidade | Invariantes principais |
|---|---|---|
| `Customer` | Cliente atendido | CNPJ válido (dígitos verificadores), e-mail e telefone válidos, endereço completo |
| `Asset` | Equipamento instalado no cliente | Tag única por cliente; garantia ≥ instalação; desativado não muda de status |
| `ServiceType` | Catálogo de serviços | SLA Crítica ≤ Alta ≤ Média ≤ Baixa; duração 15–1440 min |
| `Part` | Catálogo de peças | Preço ≥ 0; baixa de estoque nunca deixa saldo negativo |
| `Technician` | Técnico de campo | Habilidades sem duplicidade; jornada com fim > início |
| `Holiday` | Feriado (SLA) | Uma data por feriado |
| `User` | Login (Fase 2) | E-mail único; hash obrigatório |
| **`WorkOrder`** | Ordem de serviço | Máquina de estados, pré-condições de conclusão, timeline imutável |

Agregados se referenciam **apenas por Id** (FK no banco, sem navegação), exceto os filhos de `WorkOrder`
(`TimeEntry`, `UsedPart`, `ChecklistItem`, `Attachment`, `WorkOrderEvent`), que só existem dentro dela.

## Máquina de estados da OS

```
Draft ──► Open ──► Assigned ──► InProgress ──► Completed
           │         │  ▲ (reagendar)  │   ▲
           │         └──┘              ▼   │
           │         │              OnHold ┘
           ▼         ▼                 │
       Cancelled ◄───┴─────────────────┘
```

| Método | De | Para | Pré-condições / efeitos |
|---|---|---|---|
| `Create` | — | Draft | Ativo do cliente e não desativado; congela `SlaHours` (tipo × prioridade) |
| `Open` | Draft | Open | Calcula `DueAt` pelo `SlaCalculator` |
| `Assign` | Open, Assigned | Assigned | Técnico com a habilidade; janela válida (conflito/jornada: handler, Fase 5) |
| `Start` | Assigned | InProgress | Registra `StartedAt` |
| `Hold` | InProgress | OnHold | Justificativa obrigatória; congela o relógio do SLA |
| `Resume` | OnHold | InProgress | Soma `TotalHoldDuration` e estende `DueAt` pelo tempo **comercial** pausado |
| `Complete` | InProgress | Completed | Técnico atribuído, ≥ 1 apontamento, checklist obrigatório respondido |
| `Cancel` | Open, Assigned, OnHold | Cancelled | Justificativa obrigatória |

- Transição fora da tabela → `WORK_ORDER_INVALID_TRANSITION` (409).
- Qualquer comando em OS `Completed`/`Cancelled` → `WORK_ORDER_IMMUTABLE` (409).
- Pré-condição de negócio não atendida → 422 (ex.: `WORK_ORDER_REQUIRED_CHECKLIST_PENDING`).
- Toda operação bem-sucedida gera um `WorkOrderEvent` (tipo, autor, instante, de → para, payload JSON).

## SLA

`SlaCalculator` é um domain service puro:

- `CalculateDueAt(openedAt, horas)` — conta só horário comercial (seg–sex 08–18 por padrão, fuso configurável),
  pulando fins de semana e feriados. Ex.: sexta 17:00 + 4h → segunda 11:00.
- `BusinessTimeBetween(a, b)` — tempo comercial decorrido.
- `Evaluate(openedAt, dueAt, referência)` → `OnTime` | `AtRisk` (> 80% consumido) | `Breached` (passou do prazo).
  **Nunca persistido** — calculado na consulta. Em pausa, a referência é o início da pausa.
