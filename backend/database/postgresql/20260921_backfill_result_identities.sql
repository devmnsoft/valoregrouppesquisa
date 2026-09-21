-- Materialize stable result identities for scores calculated before result linking existed.
INSERT INTO valorapesquisa.results (organization_id, response_id, result_score_id, created_at)
SELECT rs.organization_id, rs.response_id, rs.id, rs.created_at
  FROM valorapesquisa.result_scores rs
  JOIN valorapesquisa.responses response
    ON response.id = rs.response_id
   AND response.organization_id = rs.organization_id
 WHERE NOT EXISTS (
       SELECT 1
         FROM valorapesquisa.results existing
        WHERE existing.response_id = rs.response_id
 );

-- SaveResultAsync relies on one stable identity per response for idempotent recalculation.
CREATE UNIQUE INDEX IF NOT EXISTS ux_results_response_id
    ON valorapesquisa.results(response_id);
