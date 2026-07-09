const {read,ok}=require('./legacy-final-validator-lib');const c=read('style.css');
['box-sizing: border-box','overflow-x: hidden','.public-result-actions','.report-actions','.confirm-actions','@media (max-width: 760px)'].forEach(x=>ok(c.includes(x),`css mobile sem ${x}`));
console.log('legacy mobile report layout: PASS');
