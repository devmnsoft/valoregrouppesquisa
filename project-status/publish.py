#!/usr/bin/env python3
"""Write only status JSON to a dedicated branch with the job token."""
import base64,json,os,urllib.request,urllib.error,pathlib,sys
base='https://api.github.com/repos/'+os.environ['GITHUB_REPOSITORY'];branch='portal-status'
headers={'Authorization':'Bearer '+os.environ['GH_TOKEN'],'Accept':'application/vnd.github+json','X-GitHub-Api-Version':'2022-11-28','User-Agent':'Valora-Project-Status'}
def request(path,method='GET',payload=None,missing=False):
 req=urllib.request.Request(base+path,data=None if payload is None else json.dumps(payload).encode(),headers=headers,method=method)
 try:
  with urllib.request.urlopen(req,timeout=30) as r:return json.load(r)
 except urllib.error.HTTPError as e:
  if missing and e.code==404:return None
  raise RuntimeError('Status publication API failed: HTTP '+str(e.code)) from None
file=pathlib.Path(sys.argv[1]);data=json.loads(file.read_text())
if request('/git/ref/heads/'+branch,missing=True) is None:request('/git/refs','POST',{'ref':'refs/heads/'+branch,'sha':data['sourceCommit']})
existing=request('/contents/project-status/status.json?ref='+branch,missing=True)
payload={'message':'docs: refresh project status '+data['sourceCommit'][:8],'branch':branch,'content':base64.b64encode(file.read_bytes()).decode()}
if existing:payload['sha']=existing['sha']
result=request('/contents/project-status/status.json','PUT',payload)
check=request('/contents/project-status/status.json?ref='+branch)
assert check['sha']==result['content']['sha'],'Readback mismatch'
print('Published and verified project status on portal-status')
