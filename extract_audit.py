import re
import os
from pathlib import Path

txt_path = r'C:\Users\Oramparo\Documents\GitHub\ProyectoFinalABS\Proyecto_final_Extract.txt'
html_path = r'C:\Users\Oramparo\.gemini\antigravity-cli\brain\96838283-61aa-4295-85f1-a2b8bd713c2a\resumen-auditoria.html'
src_dir = r'C:\Users\Oramparo\Documents\GitHub\ProyectoFinalABS'

with open(txt_path, 'r', encoding='utf-8') as f:
    text = f.read()

# Extract messages
messages = set(re.findall(r'“(.*?)”', text))

html_content = '''<!doctype html>
<title>Resumen Auditoría</title>
<style>
:root{
  --paper:#F4F6F4; --paper-raised:#FFFFFF; --ink:#182420; --ink-soft:#4B5A54;
  --line:#DCE3DE; --accent:#1F6E5C; --accent-soft:#E4EFEB;
  --gold:#8C6A1E; --gold-soft:#F5ECD8; --brick:#A23B32; --brick-soft:#F5E4E1;
  --mono-bg:#EEF2F0;
  --serif: ui-serif, "Iowan Old Style", "Source Serif 4", Georgia, "Times New Roman", serif;
  --sans: ui-sans-serif, "Segoe UI", system-ui, -apple-system, Helvetica, Arial, sans-serif;
  --mono: ui-monospace, "Cascadia Code", "Consolas", "SFMono-Regular", Menlo, monospace;
}
@media (prefers-color-scheme: dark){
  :root:not([data-theme="light"]){
    --paper:#121815; --paper-raised:#182420; --ink:#E9EFEB; --ink-soft:#A9B7B0;
    --line:#2A3630; --accent:#4FBFA0; --accent-soft:#1B332C;
    --gold:#D9A83F; --gold-soft:#332A14; --brick:#E08579; --brick-soft:#3A211E;
    --mono-bg:#1B2420;
  }
}
*{box-sizing:border-box}
body{
  background:var(--paper); color:var(--ink); font-family:var(--sans);
  margin:0; line-height:1.55; -webkit-font-smoothing:antialiased;
}
.wrap{max-width:920px; margin:0 auto; padding:2.5rem 1.5rem 6rem}
header.doc{border-bottom:2px solid var(--ink); padding-bottom:1.5rem; margin-bottom:2rem}
.eyebrow{
  font-family:var(--mono); font-size:.72rem; letter-spacing:.12em; text-transform:uppercase;
  color:var(--accent); margin:0 0 .6rem;
}
h1{
  font-family:var(--serif); font-size:2.1rem; margin:0 0 .4rem; font-weight:600;
  text-wrap:balance; letter-spacing:-.01em;
}
.subtitle{color:var(--ink-soft); font-size:1rem; max-width:62ch; margin:0}
.meta-row{display:flex; flex-wrap:wrap; gap:1.2rem; margin-top:1.2rem; font-size:.85rem; color:var(--ink-soft)}
.meta-row strong{color:var(--ink); font-weight:600}
.progress-card{
  background:var(--paper-raised); border:1px solid var(--line); border-radius:10px;
  padding:1.1rem 1.3rem; margin:1.5rem 0 2.2rem; display:flex; align-items:center; gap:1.2rem;
}
.progress-num{font-family:var(--serif); font-size:1.9rem; font-weight:600; white-space:nowrap}
.progress-bar-track{flex:1; height:8px; border-radius:99px; background:var(--line); overflow:hidden}
.progress-bar-fill{height:100%; background:var(--accent); border-radius:99px}
.progress-label{font-size:.8rem; color:var(--ink-soft); white-space:nowrap}
nav.toc{
  display:flex; flex-wrap:wrap; gap:.4rem .5rem; margin-bottom:2.5rem;
  font-family:var(--mono); font-size:.75rem;
}
nav.toc a{
  color:var(--ink-soft); text-decoration:none; border:1px solid var(--line);
  border-radius:6px; padding:.3rem .6rem; background:var(--paper-raised);
}
nav.toc a:hover{border-color:var(--accent); color:var(--accent)}
section.block{margin-bottom:2.6rem}
.block-head{display:flex; align-items:baseline; gap:.7rem; margin-bottom:.3rem}
.block-id{font-family:var(--mono); font-size:.8rem; color:var(--accent); font-weight:600}
h2{font-family:var(--serif); font-size:1.35rem; margin:0; font-weight:600}
.block-desc{color:var(--ink-soft); font-size:.92rem; margin:.3rem 0 1.1rem; max-width:70ch}
.req{
  display:grid; grid-template-columns:auto 1fr auto; gap:.9rem;
  align-items:start; padding:.85rem 0; border-top:1px solid var(--line);
}
.req:last-child{border-bottom:1px solid var(--line)}
.req-id{font-family:var(--mono); font-size:.78rem; color:var(--ink-soft); padding-top:.15rem}
.req-body p{margin:0 0 .3rem; font-size:.94rem}
.exact{
  display:inline-block; font-family:var(--mono); font-size:.82rem; background:var(--mono-bg);
  border-left:3px solid var(--brick); padding:.35rem .6rem; margin-top:.35rem; border-radius:0 5px 5px 0;
}
.exact::before{content:"“"; color:var(--brick)}
.exact::after{content:"”"; color:var(--brick)}
.pill{
  font-family:var(--mono); font-size:.7rem; letter-spacing:.03em; text-transform:uppercase;
  padding:.3rem .55rem; border-radius:99px; white-space:nowrap; font-weight:600; height:fit-content;
}
.pill.pend{background:var(--gold-soft); color:var(--gold)}
.pill.done{background:var(--accent-soft); color:var(--accent)}
.pill.verif{background:var(--accent); color:var(--paper-raised)}
</style>
<div class="wrap">
  <header class="doc">
    <p class="eyebrow">Artemis Banking Pro</p>
    <h1>Resumen Auditoría</h1>
  </header>
  <div class="progress-card">
    <div class="progress-num" id="progress-num">0/0</div>
    <div class="progress-bar-track"><div class="progress-bar-fill" id="progress-fill" style="width:0%"></div></div>
    <div class="progress-label">requerimientos implementados</div>
  </div>
  <nav class="toc">
'''

rules = [{'id': f'R{i+1}', 'exact': m} for i, m in enumerate(messages) if len(m) > 10]

html_content += '''
<a href="#General">General</a>
</nav>
<section class="block" id="General">
  <div class="block-head"><span class="block-id">1</span><h2>Mensajes y Validaciones (Extraídos)</h2></div>
'''

for r in rules:
    found = False
    for root, dirs, files in os.walk(src_dir):
        if 'bin' in root or 'obj' in root or '.git' in root: continue
        for file in files:
            if file.endswith('.cs') or file.endswith('.cshtml'):
                try:
                    with open(os.path.join(root, file), 'r', encoding='utf-8') as f:
                        if r['exact'] in f.read():
                            found = True
                            break
                except:
                    pass
        if found: break
    
    pill = '<span class="pill done">Implementado</span>' if found else '<span class="pill pend">Pendiente</span>'
    html_content += f'''
    <div class="req">
      <div class="req-id">{r['id']}</div>
      <div class="req-body">
        <p>Mensaje de error exacto:</p>
        <div class="exact">{r['exact']}</div>
      </div>
      {pill}
    </div>
    '''

html_content += '''
</section>
</div>
<script>
(function(){
  var pills = document.querySelectorAll('.pill');
  var done = 0;
  pills.forEach(function(p){ if(p.classList.contains('done') || p.classList.contains('verif')) done++; });
  var total = pills.length;
  document.getElementById('progress-num').textContent = done + '/' + total;
  document.getElementById('progress-fill').style.width = (total? (done/total*100) : 0) + '%';
})();
</script>
'''

with open(html_path, 'w', encoding='utf-8') as f:
    f.write(html_content)

print('Extracted and written HTML successfully.')
