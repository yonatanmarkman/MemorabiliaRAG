# MemorabiliaRAG

A Retrieval-Augmented Generation (RAG) application built over a real personal collection — 524 autographed items (CDs, photos, memorabilia) — exposing both a console interface and a web API.

Built as a self-directed learning project to gain hands-on experience with embeddings, semantic/hybrid search, and LLM-based generation, after identifying a gap between AI-engineering interest and hands-on AI/LLM experience during a job search.

## What it does

Ask a natural-language question about the collection (e.g. *"What items do I have signed by Metallica?"*) and get a grounded, accurate answer — generated only from items actually retrieved from the collection, with explicit source citation, rather than from the model's general knowledge.

## Architecture

```
MemorabiliaRAG.Core/       Business logic — embeddings, retrieval, generation, data loading
MemorabiliaRAG.Console/    Console interface — interactive Q&A loop
MemorabiliaRAG.Api/        ASP.NET minimal API — /health, /items, /items/{id}, /ask
```

`Core` has no knowledge of how it's being consumed — both the console app and the API are thin wrappers around the same pipeline, registered via dependency injection in the API and instantiated directly in the console app.

### Pipeline

1. **Load** — `ExcelItemLoader` (ClosedXML) reads the source `.xlsx` directly, no export step needed. Handles mixed date formats (full dates vs. bare years) present in the real data.
2. **Embed** — `EmbeddingService` generates vectors via OpenAI's `text-embedding-3-small`, batched (100 items per API call) rather than one call per item. Embeddings are cached locally (`embeddings_cache.json`) so they're generated once, not on every run.
3. **Retrieve** — `HybridSearch` combines:
   - **Semantic similarity** (cosine similarity over embeddings), weighted 0.7
   - **Keyword overlap** (word-boundary matching + phrase-match bonus), weighted 0.3
   - A combined-score threshold (empirically calibrated to 0.4) filters out irrelevant questions entirely, rather than always returning a top-5 regardless of relevance.
   - **Query decomposition**: compound questions ("Items by Accept or UDO") are split into independent sub-queries before embedding, then results are merged — a single embedded vector for a multi-entity question dilutes toward neither entity individually.
4. **Generate** — `AnswerGenerator` (OpenAI `gpt-4o-mini`, temperature 0.1) answers using only the retrieved items as context. The system prompt explicitly instructs the model to say it doesn't know rather than guess, to ignore irrelevant items even if retrieved, and to cite which specific sources it actually used ("Sources used: [1], [3]") — making generation's filtering behavior visible and checkable rather than a black box.

## Setup

1. Add `appsettings.json` (Console and Api project roots, not committed):
   ```json
   { "OpenAI": { "ApiKey": "your-key-here" } }
   ```
2. Run the console app first to generate `embeddings_cache.json` (first run only — subsequent runs load from cache).
3. Run the API project; it requires an existing cache file and will not generate one itself (a design choice — see below).

## Key design decisions

- **Core/Console/Api split**: business logic is UI-agnostic from the start, so the same pipeline serves a console loop and an HTTP API without duplication.
- **Batched embedding generation**: 524 items embedded in 6 API calls instead of 524, reducing latency and avoiding rate-limit pressure.
- **Local embedding cache**: avoids re-embedding (and re-paying for) unchanged data on every run. No cache invalidation yet — a known limitation (see below).
- **API doesn't self-generate its cache**: a web server silently making slow, costly, external-API-dependent calls as a side effect of starting up is fragile. The API is a pure consumer of a cache built by the console app.
- **All pipeline services registered as singletons** in the API: none of `EmbeddingService`, `HybridSearch`, or `AnswerGenerator` hold mutable per-request state — they take inputs, return outputs, and share only read-only data (`embeddedItems`), so one shared instance per service is safe across concurrent requests.
- **Low temperature (0.1)** for generation: this is a grounded, fact-reporting tool, not a creative one — low temperature favors consistent, literal answers over varied phrasing.
- **`gpt-4o-mini` over `gpt-4o`**: the task (context-following, not complex reasoning) doesn't need a larger model; a deliberate cost/performance choice.
- **Regex-based query decomposition over LLM-based**: minimizes latency, cost, and dependency on an external call for a well-defined, common pattern (explicit "or"/"and"). The LLM is reserved for the step that actually requires judgment — generation — not used as a default tool for everything.

## Known limitations (found through testing, not theoretical)

- **Missing information isn't retrievable.** *(e.g. "Which items were signed by Burton C. Bell?")* Embeddings only capture what's actually written in the source data. If a fact (like a band member's identity) was never recorded in the item's text, no amount of retrieval sophistication can surface it. Fix would require enriching the source data or adding an external lookup — not a retrieval-tuning problem.
- **Fixed, minimal stopword list.** Deliberately kept small — aggressive filtering (e.g. excluding "band," "group") would improve ranking precision on this dataset but risks false negatives on edge cases (a band literally named "Band" or "Group"). A static list also can't distinguish meaningful nouns from generic verbs (e.g. "get," "greets" can slip through) without true NLP (part-of-speech tagging).
- **Structured / aggregate queries are unsupported by design.** *(e.g. "What's the oldest item I have?", "What did I get most recently?", "How many items were signed in 2015?")* Embedding search answers "which items resemble this text?", not "sort / filter / count over fields." Such questions return no results because nothing in an item's text expresses ordering or date arithmetic. Not fixable by tuning thresholds or weights; it needs a separate structured-query path (see roadmap). Related data caveats: signing dates have mixed precision (full dates vs. bare years), and the dataset records signing date, not acquisition date, so "oldest" would really mean "earliest signed."
- **No cache invalidation.** Editing the source spreadsheet doesn't invalidate `embeddings_cache.json` — stale embeddings would silently persist until the cache file is manually deleted. Acceptable for a single-user learning project; a real system would hash the source file or item text to detect changes.
- **No true ranked confidence beyond the threshold.** Relevance is currently binary in effect (above 0.4 combined score = considered; below = excluded) rather than surfaced to the end user as a graded confidence level.

## Bugs found and fixed during development

- **Substring false-positives in keyword matching.** Original `Contains`-based keyword scoring matched substrings, not whole words — e.g. searching "Fear Factory" incorrectly scored "U.D.O **Steel**factory" as a partial match, since "factory" is a literal substring of "Steelfactory." Fixed with word-boundary regex matching (`\b[\w']+\b`) to extract actual whole words before comparison.
- **Keyword-order/co-occurrence blindness.** Multi-word queries scored keywords independently, so an item containing only one of several query terms (e.g. "Fear" from "Primal Fear," not "Factory") could score misleadingly high. Fixed by adding a phrase-match bonus: items containing the full adjacent phrase score higher than items with scattered, unrelated keyword hits.
- **Compound/disjunctive questions returning nothing.** *(e.g. "Items by Accept or UDO")* A single embedding vector for a multi-entity question sits semantically "between" both entities and close to neither — diluting both semantic and keyword scores below threshold even when correct answers exist for each entity individually. Fixed with regex-based query decomposition: explicit "or"/"and" split the question into independent sub-queries, each embedded and searched separately, then merged.
- **Input trimming.** `Console.ReadLine()` only reads to the first newline, silently dropping the rest of a pasted multi-line question or returning empty input. Fixed with trim + re-prompt loop.
- **High-frequency term dilution in embeddings.** Query terms present in nearly every item (e.g. "signed," in ~all 524 items) were filtered out of keyword scoring by the stopword list but still embedded as part of the raw query text, uniformly inflating semantic scores across unrelated items (e.g. "Items signed by Metallica?" surfacing items just for containing "signed"). Fixed by applying the same keyword extraction/stopword filtering before generating the query embedding, with a fallback to the raw text if filtering removes every word. Verified against the original failing query.
- **Irrelevant questions returning results regardless of actual relevance.** Cosine similarity always returns *some* score, even for genuinely unrelated text — raw top-K retrieval with no threshold returned "best available" matches even for nonsense queries. Fixed by adding an empirically-calibrated minimum similarity threshold (~0.4, determined by testing known-relevant vs. known-irrelevant questions and finding the score gap between them) rather than assuming a theoretical cutoff.

## What this isn't

- **Not an AI agent.** This is a fixed, linear pipeline (embed → retrieve → generate) — the model never decides what to do next or calls tools dynamically. A genuinely agentic version would let the model decide things like "these results are thin, let me search again" on its own.
- **Not necessarily justified by scale alone.** At 524 short items, the entire dataset would plausibly fit inside a single LLM's context window — meaning RAG isn't *strictly* necessary at this scale. Its real value here is architectural: constant cost/latency regardless of corpus size, reusable indexed data instead of re-pasting a dataset into every conversation, and a foundation that scales to far larger or multi-source data than a single prompt ever could.

## Roadmap / not yet built

- Reranking — a second retrieval pass re-scoring top candidates for relevance before final selection
- A real vector store (in-memory list only scales down, not up)
- External enrichment — looking up facts not present in the source data (e.g. band lineup history) via a real external API
- LLM-as-judge evaluation — an automated check for whether generated answers are actually grounded in their cited sources
- Conversation memory for follow-up questions
- Structured-query routing — a deterministic (regex + LINQ) path for date, ordering, and count questions that bypasses embeddings, with the result optionally passed to generation for a natural-language answer. Needs a rule for comparing mixed-precision dates; scoping pending.
