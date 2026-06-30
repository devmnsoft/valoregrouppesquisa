# Public survey contract map

## Single preparation contract
Every public survey link must be prepared by `preparePublicSurveyLink`, which delegates to `preparePublicSurveyDocument`.

Valid prepared survey document example:

```json
{
  "id": "survey_public_123",
  "companyId": "org_123",
  "organizationId": "org_123",
  "formId": "form_123",
  "title": "Diagnóstico público",
  "status": "published",
  "visibility": "public",
  "publicToken": "PUBLIC_TOKEN_ONLY",
  "token": "PUBLIC_TOKEN_ONLY",
  "tokenHash": "sha256(publicToken)",
  "showResult": true,
  "allowRepeat": true,
  "revoked": false,
  "revokedAt": null,
  "featuredOnHome": true,
  "isFeatured": true,
  "homeFeatured": true,
  "visibleOnHome": true,
  "isFree": true,
  "planId": "free"
}
```

## Final URL shapes
- Featured home survey: `https://valoragroup.mnsoft.com.br/?survey=<surveyId>&token=<publicToken>&org=<orgSlug>`
- Instant survey: `https://valoragroup.mnsoft.com.br/?survey=<surveyId>&token=<publicToken>&org=<orgSlug>`

`tokenHash` is stored only for verification and must never be returned in diagnostics or public URLs.

## Runtime functions
- `preparePublicSurveyLink`: authenticated preparation for admin/consultant/company roles.
- `repairFeaturedHomeSurvey`: admin wrapper over the same helper with `featured: true` and `free: true`.
- `getFeaturedHomeSurvey`: diagnoses candidates, auto-repairs repairable featured surveys, and returns structured `rejectedReasons` on failure.
- `validateSurveyLink`: validates public token, rejects tokenHash/demo links, and returns sanitized survey/form/company/LGPD payloads.
- `submitSurveyResponse`: public/visitor-safe submit that records a response, returns `responseId` and `resultToken`, and treats e-mail as best effort.
- `getPublicResult`: public result lookup by `responseId` + `resultToken`.
