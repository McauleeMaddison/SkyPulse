import Foundation
import ImageIO
import CoreGraphics
import CryptoKit
// Re-measure shape geometry after an intentional source-art change. No PNG is edited.
// Usage: swift bake_bird_shapes.swift PATH/TO/roster OUTPUT.json
// Flap poses 01-06 keep the connected bird and exhaust. Hit/unlock poses 07-08
// intentionally retain their original detached sparks and are never trimmed.
guard CommandLine.arguments.count==3 else {fatalError("Usage: bake_bird_shapes.swift roster_directory output.json")}
let root=URL(fileURLWithPath:CommandLine.arguments[1])
let files=try FileManager.default.contentsOfDirectory(at:root,includingPropertiesForKeys:nil).filter{$0.pathExtension=="png"}.sorted{$0.lastPathComponent<$1.lastPathComponent}
var records=[[String:Any]]();var coverageRecords=[[String:Any]]();var frames=[[String:Any]]()
for p in files where p.lastPathComponent.range(of:#"frame-0[1-6](-v1)?\.png$"#, options:.regularExpression) != nil {
 guard let src=CGImageSourceCreateWithURL(p as CFURL,nil), let im=CGImageSourceCreateImageAtIndex(src,0,nil) else { continue }
 let w=im.width,h=im.height,n=w*h
 var pixels=[UInt8](repeating:0,count:n*4)
 pixels.withUnsafeMutableBytes { ptr in
  let ctx=CGContext(data:ptr.baseAddress,width:w,height:h,bitsPerComponent:8,bytesPerRow:w*4,space:CGColorSpaceCreateDeviceRGB(),bitmapInfo:CGImageAlphaInfo.premultipliedLast.rawValue | CGBitmapInfo.byteOrder32Big.rawValue)!
  ctx.draw(im,in:CGRect(x:0,y:0,width:w,height:h))
 }
 var seen=[Bool](repeating:false,count:n),components=[[String:Any]]();var mainIndices=[Int]()
 for seed in 0..<n where !seen[seed] && pixels[seed*4+3]>=32 {
  var q=[Int]();q.append(seed);seen[seed]=true
  var read=0,x0=w,y0=h,x1=0,y1=0,peak=0
  while read<q.count {
   let at=q[read];read+=1;let x=at%w,y=at/w
   x0=min(x0,x);y0=min(y0,y);x1=max(x1,x);y1=max(y1,y);peak=max(peak,Int(pixels[at*4+3]))
   for dy in -1...1 { for dx in -1...1 {
    if dx==0 && dy==0 {continue};let a=x+dx,b=y+dy
    if a<0 || a>=w || b<0 || b>=h {continue};let k=b*w+a
    if !seen[k] && pixels[k*4+3]>=32 {seen[k]=true;q.append(k)}
   }}
  }
  if q.count>mainIndices.count {mainIndices=q}
  if q.count>=10 {components.append(["pixels":q.count,"bounds":[x0,y0,x1+1,y1+1],"peakAlpha":peak])}
 }
 components.sort{($0["pixels"] as! Int)>($1["pixels"] as! Int)}
 records.append(["file":p.lastPathComponent,"width":w,"height":h,"components":components])
 var bits=[UInt8](repeating:0,count:384)
 for index in mainIndices {
  let x=index%w,y=index/w
  for dy in -1...1 {for dx in -1...1 {
   let px=x+dx,py=y+dy;if px<0 || px>=w || py<0 || py>=h {continue}
   let col=min(63,px*64/w),row=min(47,py*48/h),cell=row*64+col
   bits[cell/8] |= UInt8(1 << (cell%8))
  }}
 }
 var mainMask=[Bool](repeating:false,count:n)
 for index in mainIndices {mainMask[index]=true}
 var retainedMain=0,retainedStray=0,totalStray=0
 for index in 0..<n where pixels[index*4+3]>=32 {
  let col=(index%w)*64/w,row=(index/w)*48/h,cell=row*64+col
  let kept=(bits[cell/8] & UInt8(1 << (cell%8))) != 0
  if mainMask[index] {if kept {retainedMain+=1}} else {totalStray+=1;if kept {retainedStray+=1}}
 }
 coverageRecords.append(["retainedMain":retainedMain,"retainedStray":retainedStray,"totalStray":totalStray,"file":p.lastPathComponent,"width":w,"height":h,"columns":64,"rows":48,"rowOrder":"top-to-bottom","coverage":Data(bits).base64EncodedString(),"mainPixels":mainIndices.count])
 let hash=SHA256.hash(data:try Data(contentsOf:p)).map{String(format:"%02x",$0)}.joined()
 frames.append(["path":"SkyPulse/characters/roster/"+p.deletingPathExtension().lastPathComponent,"width":w,"height":h,"sourceSha256":hash,"cells":Data(bits).base64EncodedString()])
 print(p.lastPathComponent,"main",mainIndices.count,"retained",retainedMain,"detached removed",totalStray-retainedStray)
}
guard frames.count==90 else {fatalError("Expected fifteen birds with six flap poses")}
let document:[String:Any]=["version":1,"columns":64,"rows":48,"frames":frames]
let data=try JSONSerialization.data(withJSONObject:document,options:[.sortedKeys,.withoutEscapingSlashes])
try data.write(to:URL(fileURLWithPath:CommandLine.arguments[2]))
print("Preserved all",coverageRecords.reduce(0){$0+($1["retainedMain"] as! Int)},"connected bird pixels; original PNG files unchanged")
