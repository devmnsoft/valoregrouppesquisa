const {ok,fn}=require('./_legacy-final-validators');
const m=fn.match(/exports\.getPublicResult[\s\S]*?return \{ok:true/);const b=m?m[0]:'';
ok(/resultToken/.test(b)&&/sha256\(resultToken\)/.test(b)&&/resultTokenHash/.test(b),'getPublicResult validates token hash');
ok(!/authedUser\(|req\.auth/.test(b),'getPublicResult does not require req.auth');
ok(/delete response\.resultTokenHash/.test(fn),'resultTokenHash is not returned');
