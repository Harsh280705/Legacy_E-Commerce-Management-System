import hashlib, re
# Generates db/seed_1500.sql: 1500 NEW distinct products (MASS- namespace).
# Idempotent: every INSERT guarded by IF NOT EXISTS on Slug. Never deletes.
LEAF = {  # leaf-category slug -> (image, variant sizes or None)
 'men-fashion':('/Images/fashion.png',['S','M','L','XL']),
 'women-fashion':('/Images/fashion.png',['XS','S','M','L']),
 'kids-fashion':('/Images/fashion.png',['2-3Y','4-5Y','6-7Y','8-9Y']),
 'men-footwear':('/Images/footwear.png',['UK 6','UK 7','UK 8','UK 9']),
 'women-footwear':('/Images/footwear.png',['UK 3','UK 4','UK 5','UK 6']),
 'decor':('/Images/decor.png',None),
 'kitchen':('/Images/home.png',None),
 'audio':('/Images/tech.png',None),
 'mobile-accessories':('/Images/tech.png',None),
 'fitness':('/Images/sports.png',None),
 'beauty':('/Images/beauty.png',None),
 'books':('/Images/books.png',None),
 'toys':('/Images/toys.png',None),
 'fashion':('/Images/fashion.png',None),
 'footwear':('/Images/footwear.png',None),
 'home-kitchen':('/Images/home.png',None),
 'electronics':('/Images/tech.png',None),
 'sports':('/Images/sports.png',None),
}
BASE_NAMES = {
 'men-fashion':['Oxford Shirt','Polo T-Shirt','Cargo Pants','Nehru Jacket','Track Pants','Denim Shirt','Kurta Pyjama Set','Waistcoat','Sweatshirt','Formal Trousers'],
 'women-fashion':['A-Line Kurti','Maxi Dress','Denim Jacket','Silk Blouse','Wrap Top','Straight Pants','Tunic Top','Skater Skirt','Cardigan','Leggings Pair'],
 'kids-fashion':['Graphic Tee','Jeans','Frock','Shorts','Sweater','Track Suit','Shirt','Skirt Set','Night Suit','Jacket'],
 'men-footwear':['Derby Shoes','High-Top Sneakers','Chappals','Sports Sandals','Chelsea Boots','Slip-Ons','Court Shoes','Floaters'],
 'women-footwear':['Kitten Heels','Sports Shoes','Kolhapuris','Loafers','Ankle Boots','Flip-Flops','Mary Janes','Knitted Sneakers'],
 'decor':['Table Runner','Wall Clock','Photo Frame Set','Cushion Covers','Bedside Lamp','Mirror Frame','Vase','Curtain Pair'],
 'kitchen':['Pressure Cooker','Frying Pan','Knife Set','Lunch Box','Water Bottle','Spice Box','Apron Set','Serving Tray'],
 'audio':['Earbuds Lite','Headphones Studio','Bluetooth Speaker Mini','Soundbar','Microphone','Earphones Sport','Turntable','Radio Retro'],
 'mobile-accessories':['Charger Cable','Wireless Charger','Earbuds Case','Phone Stand','Car Mount','OTG Adapter','Screen Guard','Selfie Stick'],
 'fitness':['Dumbbells Pair','Kettlebell','Pull-Up Bar','Ankle Weights','Gym Bag','Towel Set','Jump Rope','Exercise Ball'],
 'beauty':['Face Serum','Sunscreen SPF50','Hair Conditioner','Body Lotion','Face Mask Pack','Lip Balm Duo','Nail Polish Set','Toner'],
 'books':['Mystery Novel','History of India','Science Encyclopedia','Poetry Collection','Business Bestseller','Comic Collection','Atlas','Dictionary'],
 'toys':['Action Figure','Building Set','Teddy Bear','Chess Set','Cricket Kit','Doll Set','Robot Toy','Art Kit'],
 'fashion':['Scarf Set','Belt','Wallet','Cap','Sunglasses','Handkerchief Pack','Socks Pack','Tie Set'],
 'footwear':['Shoe Polish Kit','Insoles Pair','Laces Pack','Shoe Bag','Waterproof Spray','Heel Grips','Shoe Horn','Deodorizer'],
 'home-kitchen':['Doormat','Storage Box Set','Laundry Basket','Dustbin','Mop Set','Bucket','Cloth Hanger','Dinner Mats'],
 'electronics':['Extension Board','LED Bulb Pack','Torch','Calculator','Table Fan','Iron Box','Kettle Mini','Trimmer'],
 'sports':['Football','Badminton Set','Tennis Balls','Cricket Bat','Skipping Rope','Water Bottle Sport','Cap Sport','Wrist Band'],
}
COLORS=['Red','Blue','Green','Black','White','Maroon','Navy','Beige','Grey','Teal','Mustard','Olive','Pink','Brown','Charcoal']
def slug(s):
    s=s.lower(); s=re.sub(r"[^a-z0-9]+","-",s); return s.strip('-')
def q(s): return s.replace("'","''")
def gen():
    out=["-- MASS-1500 seed: 1500 NEW products. Idempotent (IF NOT EXISTS on Slug).",
         "-- Namespace: Slug/Sku MASS-*. Never touches existing rows, customers, orders or carts.",
         "USE LegacyEcommerceDb;","GO"]
    n=0; k=0
    prod_lines=[]; var_lines=[]
    while n<1500:
        for cat,names in BASE_NAMES.items():
            if n>=1500: break
            base=names[k%len(names)]; color=COLORS[(k//len(names)+n)%len(COLORS)]
            name="%s %s %d"%(color,base,1000+n)
            s="mass-%s-%d"%(slug("%s %s"%(color,base)),1000+n)
            sku="MASS-%05d"%(1000+n)
            h=int(hashlib.md5(name.encode()).hexdigest()[:8],16)
            price=round(99+(h%5950)+((h%100)/100.0),2)
            cmp_price=round(price*1.3,2) if n%4==0 else None
            stock=3+(h%200)
            feat=1 if n%25==0 else 0
            new=1 if n%7==0 else 0
            img,sizes=LEAF[cat]
            cmp='NULL' if cmp_price is None else str(cmp_price)
            cs="(SELECT CategoryId FROM dbo.Categories WHERE Slug='%s')"%cat
            prod_lines.append("IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Slug='%s')"%s)
            prod_lines.append("INSERT INTO dbo.Products(Sku,Name,Slug,ShortDescription,Description,Price,CompareAtPrice,StockQuantity,CategoryId,IsFeatured,IsNewArrival,IsActive,CreatedUtc) VALUES (N'%s',N'%s',N'%s',N'Demo %s - quality checked.',N'Mass-catalogue demo product %s in %s.',%s,%s,%d,%s,%d,%d,1,SYSUTCDATETIME());"%(sku,q(name),s,q(name),q(name),cat,price,cmp,stock,cs,feat,new))
            prod_lines.append("IF NOT EXISTS (SELECT 1 FROM dbo.ProductImages WHERE ProductId=(SELECT ProductId FROM dbo.Products WHERE Slug='%s'))"%s)
            prod_lines.append("INSERT INTO dbo.ProductImages(ProductId,ImageUrl,AltText,DisplayOrder) SELECT ProductId,N'%s',N'%s',0 FROM dbo.Products WHERE Slug='%s';"%(img,q(name),s))
            if sizes:
                for sz in sizes:
                    var_lines.append("IF NOT EXISTS (SELECT 1 FROM dbo.ProductVariants WHERE ProductId=(SELECT ProductId FROM dbo.Products WHERE Slug='%s') AND VariantValue='%s')"%(s,sz))
                    var_lines.append("INSERT INTO dbo.ProductVariants(ProductId,VariantName,VariantValue,Sku,PriceAdjustment,StockQuantity) SELECT ProductId,N'Size',N'%s',N'%s-%s',0,%d FROM dbo.Products WHERE Slug='%s';"%(sz,sku,sz.replace(' ','-'),max(1,stock//len(sizes)),s))
            n+=1
        k+=1
    # GO batches every ~100 products (400 prod stmts) to keep sqlcmd batches small
    for i in range(0,len(prod_lines),400):
        out.extend(prod_lines[i:i+400]); out.append("GO")
    for i in range(0,len(var_lines),400):
        out.extend(var_lines[i:i+400]); out.append("GO")
    out.append("PRINT 'Seed MASS-1500 OK';")
    return "\n".join(out)
open('db/seed_1500.sql','w',encoding='utf-8').write(gen())
print("wrote db/seed_1500.sql")
