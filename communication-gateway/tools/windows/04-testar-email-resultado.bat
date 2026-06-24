@echo off
curl -X POST http://localhost:8097/communication/result/send ^
 -H "Content-Type: application/json" ^
 -H "Authorization: Bearer trocar-por-token-forte" ^
 -d "{\"eventType\":\"survey_completed\",\"responseId\":\"resp_teste\",\"surveyId\":\"survey_teste\",\"companyId\":\"company_teste\",\"participant\":{\"name\":\"Maria Silva\",\"email\":\"maria@email.com\"},\"company\":{\"name\":\"Empresa Cliente\"},\"survey\":{\"title\":\"Diagnostico Valora Insight\"},\"result\":{\"score\":72,\"maxScore\":125,\"level\":\"Em estruturacao\",\"strongestDimension\":\"Resultados e Crescimento\",\"weakestDimension\":\"Gestao e Governanca\",\"dimensions\":[]},\"links\":{\"resultUrl\":\"https://valoragroup.mnsoft.com.br/?result=teste\",\"certificateUrl\":\"https://valoragroup.mnsoft.com.br/?certificate=teste\"}}"
pause
