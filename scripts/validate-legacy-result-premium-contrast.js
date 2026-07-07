const {ok,app,css}=require('./_legacy-final-validators');
ok(/function renderPremiumPublicResult/.test(app),'renderPremiumPublicResult exists');
ok(/#F7FCFD/i.test(css)&&/#CDEAF0/i.test(css)&&/#073F4D/i.test(css)&&/#042F3A/i.test(css),'premium contrast palette exists');
ok(/result-score-panel[\s\S]*rgba\(255,255,255,\.12\)/.test(css),'score panel translucent contrast');
