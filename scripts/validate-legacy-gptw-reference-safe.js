const {has,no}=require('./legacy-premium-validator-utils');
has(/GPTW Brasil/,'GPTW Brasil citado');
has(/Não representa certificação GPTW, ranking oficial, pontuação externa ou validação pública de mercado/,'disclaimer GPTW seguro');
no(/certificada pelo GPTW|certificação GPTW concedida|ranking oficial GPTW|pontuação GPTW real|substitui (a )?metodologia GPTW/i,'não afirma certificação/ranking/metodologia GPTW');
