# Pontuação da maturidade estratégica

`StrategicMaturityScoringService` valida valores inteiros de 1 a 5 e a presença de `VALORA_MATURITY_Q01` a `Q25`. Soma somente esses 25 itens (25–125), calcula média com uma casa decimal e classifica Atenção (25–55), Evolução (56–85), Consistência (86–110) ou Excelência (111–125).

As cinco dimensões principais exigem cinco respostas e são classificadas por 5–11, 12–17, 18–22 e 23–25. Q26/Q27 são opcionais, calculadas separadamente com máximo 10, e nunca alteram o nível principal. O endpoint autenticado de prévia é `POST /api/v1/scoring/strategic-maturity/preview`.
