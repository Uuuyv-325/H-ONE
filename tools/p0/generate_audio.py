"""Rebuild original M0/M1 technical cues, without external recordings."""
import wave,math,random,pathlib,argparse
root=pathlib.Path(__file__).resolve().parents[2]/'Assets/Game/Audio/P0';root.mkdir(parents=True,exist_ok=True);rng=random.Random(1104);sr=48000
cues={'charge_start':(.18,[293.665,440],False),'charge_loop':(1.,[293.,440.],True),'launch':(.22,[587.33,880],False),'wall':(.16,[96,143],False),'fail':(.32,[293.665,220],False),'reform':(.28,[440,587.33,880],False),'collect':(.42,[587.33,880,1174.66],False)}
parser=argparse.ArgumentParser()
parser.add_argument('--overwrite',action='store_true')
args=parser.parse_args()
if not args.overwrite and any((root/('p0_'+name+'.wav')).exists() for name in cues):
 raise SystemExit('P0 files exist; use --overwrite only when intentionally regenerating them.')
for name,(seconds,freqs,loop) in cues.items():
 n=round(sr*seconds);data=bytearray()
 for i in range(n):
  t=i/sr
  env=1 if loop else min(t/.008,1)*math.exp(-t/(seconds*.22))
  tone=sum(math.sin(2*math.pi*f*t) for f in freqs)/len(freqs)
  if name=='wall':tone=.6*rng.uniform(-1,1)+tone*.4
  if name=='launch':tone=math.sin(2*math.pi*(520*t-450*t*t))*.7+tone*.3
  amplitude=.11 if loop else .33
  value=max(-8388608,min(8388607,round(tone*env*amplitude*8388607)))
  data.extend(value.to_bytes(3,'little',signed=True))
 with wave.open(str(root/('p0_'+name+'.wav')),'wb') as w:w.setnchannels(1);w.setsampwidth(3);w.setframerate(sr);w.writeframes(data)
print('7 original synthesized technical cues, 48 kHz / 24-bit mono.')
