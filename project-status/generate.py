#!/usr/bin/env python3
"""Public project inventory; never infers acceptance from commits."""
import json,re,subprocess,pathlib,datetime,sys
ROOT=pathlib.Path(__file__).resolve().parents[1]
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT,text=True)
def build():
 plan=json.loads((ROOT/'project-status/plan.json').read_text());head=git('rev-parse','HEAD').strip()
 records=[]
 for p in git('ls-tree','-r','--name-only','HEAD').splitlines():
  if not p.lower().endswith('.md') or p.startswith('project-status/'):continue
  text=(ROOT/p).read_text(errors='replace');headings=[re.sub(r'^#+\s*','',l).strip() for l in text.splitlines() if re.match(r'^#{1,6}\s+.*\bsprints?\b',l,re.I)]
  if 'sprint' not in p.lower() and not headings:continue
  titles=[re.sub(r'^#+\s*','',l).strip() for l in text.splitlines() if re.match(r'^#\s+',l)]
  records.append({'path':p,'title':titles[0] if titles else pathlib.Path(p).stem.replace('_',' '),'headings':headings,'updatedAt':git('log','-1','--format=%cI','--',p).strip(),'state':'Documentada · aceite a conferir'})
 records.sort(key=lambda x:x['updatedAt'],reverse=True)
 try:changed=git('diff','--name-only',plan['reviewedCommit'],head).splitlines();available=True
 except subprocess.CalledProcessError:changed=[];available=False
 areas=[]
 for m in plan['areas']:
  matches=[p for p in changed if any(x.lower() in p.lower() for x in m['pathPatterns'])]
  areas.append({**m,'changedFiles':matches,'updateState':'Revisão necessária' if matches else 'Sem alteração detectada desde a revisão' if available else 'Comparação indisponível'})
 commits=[]
 for line in git('log','-100','--format=%H%x09%cI%x09%s').splitlines():
  sha,date,title=line.split('\t',2);commits.append({'sha':sha,'date':date,'title':title})
 return {**plan,'schemaVersion':1,'sourceCommit':head,'generatedAt':datetime.datetime.now(datetime.timezone.utc).isoformat(),'baselineAvailable':available,'areas':areas,'sprintDocuments':records,'recentCommits':commits,'changedFileCount':len(changed),'automation':{'mode':'push-main','acceptanceAutomatic':False}}
if __name__=='__main__':
 out=pathlib.Path(sys.argv[1] if len(sys.argv)>1 else '/tmp/valora-project-status.json');out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(build(),ensure_ascii=False,indent=2)+'\n');print('Project status generated:',out.name)
