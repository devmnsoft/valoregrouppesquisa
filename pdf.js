(function(global){
'use strict';

const CP1252 = {0x20AC:0x80,0x201A:0x82,0x0192:0x83,0x201E:0x84,0x2026:0x85,0x2020:0x86,0x2021:0x87,0x02C6:0x88,0x2030:0x89,0x0160:0x8A,0x2039:0x8B,0x0152:0x8C,0x017D:0x8E,0x2018:0x91,0x2019:0x92,0x201C:0x93,0x201D:0x94,0x2022:0x95,0x2013:0x96,0x2014:0x97,0x02DC:0x98,0x2122:0x99,0x0161:0x9A,0x203A:0x9B,0x0153:0x9C,0x017E:0x9E,0x0178:0x9F};
const encoder = new TextEncoder();

function winAnsiBytes(text){
  const out=[];
  for(const ch of String(text??'')){
    const code=ch.codePointAt(0);
    if(code===10||code===13||code===9){ out.push(32); continue; }
    if(code>=32&&code<=126){ out.push(code); continue; }
    if(code>=160&&code<=255){ out.push(code); continue; }
    if(CP1252[code]){ out.push(CP1252[code]); continue; }
    out.push(63);
  }
  return new Uint8Array(out);
}
function ascii(text){ return encoder.encode(String(text)); }
function concat(parts){
  const arrays=parts.map(p=>p instanceof Uint8Array?p:ascii(p));
  const total=arrays.reduce((n,a)=>n+a.length,0); const out=new Uint8Array(total); let offset=0;
  arrays.forEach(a=>{out.set(a,offset);offset+=a.length;}); return out;
}
function pdfLiteral(text){
  const bytes=winAnsiBytes(text); const out=[40];
  for(const b of bytes){
    if(b===40||b===41||b===92) out.push(92,b);
    else if(b<32) out.push(32);
    else out.push(b);
  }
  out.push(41); return new Uint8Array(out);
}
function cmdText(x,y,size,text,bold=false,color='0.03 0.16 0.21'){
  return concat([`BT /${bold?'F2':'F1'} ${size} Tf ${color} rg ${x.toFixed(2)} ${y.toFixed(2)} Td `,pdfLiteral(text),' Tj ET\n']);
}
function cmdLine(x1,y1,x2,y2,width=1,color='0.75 0.84 0.87'){
  return ascii(`${color} RG ${width} w ${x1} ${y1} m ${x2} ${y2} l S\n`);
}
function cmdRect(x,y,w,h,fill='1 1 1',stroke='0.05 0.24 0.30',lineWidth=1){
  return ascii(`${fill} rg ${stroke} RG ${lineWidth} w ${x} ${y} ${w} ${h} re B\n`);
}
function splitWords(text,maxChars){
  const words=String(text??'').replace(/\s+/g,' ').trim().split(' ').filter(Boolean); const lines=[]; let line='';
  for(const word of words){ const candidate=line?`${line} ${word}`:word; if(candidate.length>maxChars&&line){lines.push(line);line=word;} else line=candidate; }
  if(line) lines.push(line); return lines.length?lines:[''];
}
function addWrapped(parts,x,y,widthChars,lineHeight,size,text,bold=false,color){
  const lines=splitWords(text,widthChars); lines.forEach((line,i)=>parts.push(cmdText(x,y-i*lineHeight,size,line,bold,color))); return y-lines.length*lineHeight;
}
function blobDownload(bytes,filename){ const blob=new Blob([bytes],{type:'application/pdf'}); const url=URL.createObjectURL(blob); const a=document.createElement('a');a.href=url;a.download=filename;a.style.display='none';document.body.appendChild(a);a.click();a.remove();setTimeout(()=>URL.revokeObjectURL(url),1500);return blob; }

function buildPdf(pageContents,pageSize=[595,842]){
  const objects=[];
  objects[1]=ascii('<< /Type /Catalog /Pages 2 0 R >>');
  const pageIds=[];
  const firstPageId=5;
  pageContents.forEach((_,i)=>pageIds.push(firstPageId+i*2));
  objects[2]=ascii(`<< /Type /Pages /Kids [${pageIds.map(id=>`${id} 0 R`).join(' ')}] /Count ${pageIds.length} >>`);
  objects[3]=ascii('<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>');
  objects[4]=ascii('<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>');
  pageContents.forEach((content,i)=>{
    const pageId=firstPageId+i*2, contentId=pageId+1;
    objects[pageId]=ascii(`<< /Type /Page /Parent 2 0 R /MediaBox [0 0 ${pageSize[0]} ${pageSize[1]}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents ${contentId} 0 R >>`);
    const stream=content instanceof Uint8Array?content:concat(content);
    objects[contentId]=concat([`<< /Length ${stream.length} >>\nstream\n`,stream,'\nendstream']);
  });
  const header=ascii('%PDF-1.4\n%\xE2\xE3\xCF\xD3\n');
  const chunks=[header]; const offsets=[0]; let length=header.length;
  for(let id=1;id<objects.length;id++){
    const obj=objects[id]||ascii('<<>>'); offsets[id]=length;
    const chunk=concat([`${id} 0 obj\n`,obj,'\nendobj\n']); chunks.push(chunk); length+=chunk.length;
  }
  const xrefOffset=length;
  const xref=[`xref\n0 ${objects.length}\n0000000000 65535 f \n`];
  for(let id=1;id<objects.length;id++) xref.push(`${String(offsets[id]).padStart(10,'0')} 00000 n \n`);
  const trailer=`trailer\n<< /Size ${objects.length} /Root 1 0 R >>\nstartxref\n${xrefOffset}\n%%EOF\n`;
  chunks.push(ascii(xref.join(''))); chunks.push(ascii(trailer));
  return concat(chunks);
}

function createReport(options,filename='relatorio-valora-pulse.pdf'){
  const title=options?.title||'Relatório Valora Pulse™'; const subtitle=options?.subtitle||'';
  const metrics=Array.isArray(options?.metrics)?options.metrics:[];
  const columns=Array.isArray(options?.columns)&&options.columns.length?options.columns:[{key:'item',label:'Item',width:1}];
  const rows=Array.isArray(options?.rows)?options.rows:[];
  const pageW=595,pageH=842,margin=38,usable=pageW-margin*2;
  const totalWeight=columns.reduce((s,c)=>s+Number(c.width||1),0); const widths=columns.map(c=>usable*Number(c.width||1)/totalWeight);
  const pages=[]; let parts=[],y=pageH-margin;
  function header(pageNo){
    parts.push(ascii(`0.043 0.239 0.302 rg 0 ${pageH-92} ${pageW} 92 re f\n`));
    parts.push(cmdText(margin,pageH-48,21,'VALORA PULSE',true,'1 1 1'));
    parts.push(cmdText(margin,pageH-70,9,'Governança • Controller • Advisory',false,'0.77 0.95 0.98'));
    parts.push(cmdText(pageW-92,pageH-65,9,`Página ${pageNo}`,false,'1 1 1'));
    y=pageH-122;
  }
  function finish(){ pages.push(concat(parts)); parts=[]; }
  function ensure(space){ if(y-space<44){ finish(); header(pages.length+1); } }
  header(1);
  y=addWrapped(parts,margin,y,62,24,18,title,true,'0.03 0.16 0.21')-4;
  if(subtitle) y=addWrapped(parts,margin,y,85,16,10,subtitle,false,'0.25 0.40 0.46')-8;
  if(metrics.length){
    ensure(72); const boxW=(usable-(Math.min(metrics.length,4)-1)*8)/Math.min(metrics.length,4); let x=margin;
    metrics.slice(0,4).forEach((m,i)=>{parts.push(cmdRect(x,y-54,boxW,54,'0.93 0.98 0.99','0.62 0.79 0.83',.8));parts.push(cmdText(x+9,y-20,8,String(m.label||''),true,'0.20 0.36 0.42'));parts.push(cmdText(x+9,y-42,15,String(m.value??''),true,'0.03 0.16 0.21'));x+=boxW+8;}); y-=72;
  }
  parts.push(cmdText(margin,y,12,'Detalhamento',true)); y-=18;
  function tableHeader(){ let x=margin;parts.push(ascii(`0.90 0.96 0.97 rg ${margin} ${y-22} ${usable} 24 re f\n`));columns.forEach((c,i)=>{parts.push(cmdText(x+4,y-15,8,String(c.label||c.key),true));x+=widths[i];});y-=27; }
  tableHeader();
  if(!rows.length){ parts.push(cmdText(margin,y-12,10,'Nenhum registro disponível para este escopo.')); y-=28; }
  rows.forEach((row,rowIndex)=>{
    const cellLines=columns.map((c,i)=>splitWords(row?.[c.key]??'',Math.max(8,Math.floor(widths[i]/5.2))));
    const lines=Math.max(...cellLines.map(a=>a.length)); const rowH=Math.max(24,lines*11+8);
    if(y-rowH<44){ finish();header(pages.length+1);tableHeader(); }
    if(rowIndex%2===0) parts.push(ascii(`0.98 0.99 1 rg ${margin} ${y-rowH+3} ${usable} ${rowH} re f\n`));
    let x=margin;columns.forEach((c,i)=>{cellLines[i].slice(0,5).forEach((line,j)=>parts.push(cmdText(x+4,y-12-j*10,7.8,line,false,'0.05 0.17 0.22')));x+=widths[i];});
    parts.push(cmdLine(margin,y-rowH+3,pageW-margin,y-rowH+3,.5)); y-=rowH;
  });
  ensure(36); parts.push(cmdText(margin,32,7.8,`Gerado em ${new Date().toLocaleString('pt-BR')} • Documento confidencial`,false,'0.30 0.45 0.51'));
  finish(); const bytes=buildPdf(pages,[pageW,pageH]); return blobDownload(bytes,filename);
}

function textWidthApprox(text,size){return String(text??'').length*size*.52;}
function centerText(parts,pageWidth,y,size,text,bold=false,color='0.03 0.16 0.21'){
  const safe=String(text??'');parts.push(cmdText(Math.max(36,(pageWidth-textWidthApprox(safe,size))/2),y,size,safe,bold,color));return y-size*1.25;
}
function wrappedCenteredText(parts,pageWidth,y,maxChars,lineHeight,size,text,bold=false,color='0.03 0.16 0.21',maxLines=6){
  const lines=splitWords(text,maxChars).slice(0,maxLines);lines.forEach(line=>{centerText(parts,pageWidth,y,size,line,bold,color);y-=lineHeight;});return y;
}
function fitCenteredText(parts,pageWidth,y,maxChars,maxFont,minFont,text,color='0.043 0.239 0.302'){
  const lines=splitWords(text,maxChars).slice(0,2);let size=maxFont;if(lines.some(l=>l.length>34))size=Math.max(minFont,maxFont-6);if(lines.some(l=>l.length>48))size=minFont;lines.forEach(line=>{centerText(parts,pageWidth,y,size,line,true,color);y-=size+7;});return y;
}
function createCertificate(data,filename='certificado-valora-pulse.pdf'){
  const W=842,H=595,parts=[];
  const participant=String(data?.participantName||'Participante não identificado');
  const survey=String(data?.surveyTitle||data?.surveyName||'Diagnóstico Valora Insight™');
  const date=String(data?.completedDate||data?.formattedDate||(data?.completedAt?new Date(data.completedAt).toLocaleDateString('pt-BR'):new Date().toLocaleDateString('pt-BR')));
  const score=String(data?.scoreShortLabel||data?.scoreLabel||'Participação concluída');
  const level=String(data?.maturityLabel||'');
  const issuer=String(data?.issuedByText||data?.institutionalMessage||'Emitido por Valora Group');
  const validation=String(data?.validationText||'');
  parts.push(ascii(`0.985 0.995 1 rg 0 0 ${W} ${H} re f\n`));
  parts.push(ascii(`0.043 0.239 0.302 RG 4 w 30 30 ${W-60} ${H-60} re S\n`));
  parts.push(ascii(`0.69 0.86 0.89 RG 1.2 w 48 48 ${W-96} ${H-96} re S\n`));
  parts.push(cmdRect(66,H-122,W-132,54,'0.043 0.239 0.302','0.043 0.239 0.302',0));
  parts.push(cmdText(88,H-88,18,'VALORA GROUP™',true,'1 1 1'));
  parts.push(cmdText(W-210,H-88,10,'Valora Pulse™',true,'0.78 0.95 0.98'));
  let y=H-158;
  y=centerText(parts,W,y,11,'CERTIFICADO DE PARTICIPAÇÃO',true,'0.03 0.46 0.31')-16;
  y=centerText(parts,W,y,16,'Certificamos que',false,'0.25 0.40 0.46')-8;
  y=fitCenteredText(parts,W,y,44,30,18,participant,'0.043 0.239 0.302')-6;
  y=centerText(parts,W,y,13,'concluiu o diagnóstico',false,'0.25 0.40 0.46')-8;
  y=wrappedCenteredText(parts,W,y,70,17,13,survey,true,'0.03 0.16 0.21',3)-4;
  y=centerText(parts,W,y,12,`em ${date}`,false,'0.25 0.40 0.46')-12;
  const boxW=380,boxH=62,boxY=Math.max(116,y-boxH-6);
  parts.push(cmdRect(W/2-boxW/2,boxY,boxW,boxH,'0.93 0.98 0.99','0.45 0.79 0.84',1));
  centerText(parts,W,boxY+43,8.5,'Resultado',false,'0.25 0.40 0.46');
  centerText(parts,W,boxY+25,12.5,score,true,'0.043 0.239 0.302');
  if(level)centerText(parts,W,boxY+10,8.5,level,false,'0.03 0.46 0.31');
  wrappedCenteredText(parts,W,78,92,12,9,issuer,false,'0.25 0.40 0.46',1);
  if(validation)wrappedCenteredText(parts,W,61,100,11,8,validation,false,'0.25 0.40 0.46',1);
  centerText(parts,W,38,8.8,'Tecnologia Valora Pulse™',true,'0.043 0.239 0.302');
  const bytes=buildPdf([concat(parts)],[W,H]); return blobDownload(bytes,filename);
}

global.ValoraPDF={createReport,createCertificate,buildPdf};
})(window);
