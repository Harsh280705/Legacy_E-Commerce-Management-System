import hashlib, re
cats = [
 (1,'Fashion','fashion','Clothing for the family',None,'/Images/fashion.png',1),
 (2,'Footwear','footwear','Shoes for every occasion',None,'/Images/footwear.png',2),
 (3,'Home & Kitchen','home-kitchen','Home essentials',None,'/Images/home.png',3),
 (4,'Electronics','electronics','Audio and accessories',None,'/Images/tech.png',4),
 (5,'Beauty','beauty','Everyday beauty',None,'/Images/beauty.png',5),
 (6,'Sports','sports','Fitness and outdoor',None,'/Images/sports.png',6),
 (7,'Books','books','Fiction, study and kids',None,'/Images/books.png',7),
 (8,'Toys','toys','Toys and games',None,'/Images/toys.png',8),
 (9,'Men Fashion','men-fashion','Menswear',1,'/Images/fashion.png',1),
 (10,'Women Fashion','women-fashion','Womenswear',1,'/Images/fashion.png',2),
 (11,'Kids Fashion','kids-fashion','Kidswear',1,'/Images/fashion.png',3),
 (12,'Men Footwear','men-footwear','Men shoes',2,'/Images/footwear.png',1),
 (13,'Women Footwear','women-footwear','Women shoes',2,'/Images/footwear.png',2),
 (14,'Decor','decor','Home decor',3,'/Images/decor.png',1),
 (15,'Kitchen','kitchen','Kitchen essentials',3,'/Images/home.png',2),
 (16,'Audio','audio','Headphones and speakers',4,'/Images/tech.png',1),
 (17,'Mobile Accessories','mobile-accessories','Cases, chargers and more',4,'/Images/tech.png',2),
 (18,'Fitness','fitness','Gym and yoga',6,'/Images/sports.png',1),
]
base_products = [
 ('men-fashion',['Classic Cotton Kurta','Slim-Fit Formal Shirt','Indigo Denim Jeans','Cotton Casual T-Shirt','Festive Silk Kurta','Linen Summer Shirt','Chino Trousers','Wool Blend Blazer']),
 ('women-fashion',['Banarasi Silk Saree','Cotton Anarkali Kurti','Floral Summer Dress','Embroidered Lehenga Choli','Straight-Fit Jeans','Chiffon Evening Gown','Handloom Cotton Saree','Palazzo Set']),
 ('kids-fashion',['Kids Cotton T-Shirt Pack','Kids Denim Dungaree','Kids Festive Kurta','Kids Party Frock','Kids School Shorts Set','Kids Hooded Sweatshirt']),
 ('men-footwear',['Leather Formal Oxfords','Running Sports Shoes','Canvas Casual Sneakers','Suede Loafers','Trekking Boots','Traditional Mojari']),
 ('women-footwear',['Block-Heel Sandals','Running Shoes','Embroidered Juttis','Ballet Flats','Wedge Heels','Slip-On Sneakers']),
 ('decor',['Brass Table Lamp','Handwoven Wall Hanging','Ceramic Vase Set','Wooden Photo Frames Set of 3','Scented Soy Candles Set of 4','Macrame Plant Hangers']),
 ('kitchen',['Stainless Steel Dinner Set 24pc','Non-Stick Cookware Set 5pc','Insulated Flask 1L','Masala Dabba 7 Bowls','Cast Iron Tawa','Glass Storage Jars Set of 6']),
 ('audio',['Bluetooth Headphones Pro','True Wireless Earbuds','Portable Bluetooth Speaker','Wired Earphones with Mic','Soundbar 60W','Neckband Earphones']),
 ('mobile-accessories',['Fast Charger 25W','Braided USB-C Cable 1.5m','Phone Back Cover','Tempered Glass Pack of 2','Power Bank 20000mAh','Mobile Tripod Stand']),
 ('fitness',['Yoga Mat 6mm Anti-Skid','Dumbbell Set 10kg','Resistance Bands Set of 5','Skipping Rope Pro','Gym Shaker 700ml','Foam Roller']),
 ('beauty',['Herbal Face Wash 150ml','Matte Lipstick Set 6','Kajal Eyeliner Combo','Moisturising Cream 200ml','Hair Oil with Amla 200ml','Compact Powder']),
 ('books',['Classic Indian Fiction Bestseller','Competitive Exam Guide 2026','Children Picture Book Set','Cookbook Regional Classics','Self-Help Paperback','Notebook Set Pack of 3']),
 ('toys',['Wooden Building Blocks 100pc','Remote Control Car','Doll House Playset','Board Game Family Pack','Plush Elephant Soft Toy','Puzzle 500 Pieces']),
 ('decor',['Cotton Bedsheet King Size','Cushion Covers Set of 5']),
 ('kitchen',['Electric Kettle 1.8L','Mixer Grinder 750W']),
]
def slug(s):
    s=s.lower(); s=re.sub(r"[^a-z0-9]+","-",s); return s.strip('-')
def gen(n_target):
    prods=[]
    for cat_slug,names in base_products:
        for n in names:
            if len(prods)>=n_target: break
            prods.append((cat_slug,n))
        if len(prods)>=n_target: break
    k=1
    while len(prods)<n_target:
        for cat_slug,names in base_products:
            if len(prods)>=n_target: break
            base=names[(k-1)%len(names)]
            prods.append((cat_slug,"%s Mk%d"%(base,k)))
        k+=1
    lines=[]
    for idx,(cat_slug,name) in enumerate(prods,1):
        h=int(hashlib.md5(name.encode()).hexdigest()[:8],16)
        price=round(149 + (h%4850) + ((h%100)/100.0),2)
        cmp_price=round(price*1.25,2) if idx%3==0 else None
        stock=5+(h%120)
        feat=1 if idx%9==0 else 0
        new=1 if idx%5==0 else 0
        s=slug(name) if n_target<=200 else slug(name)+("-%d"%idx)
        sku="DEMO-%05d"%idx
        imgmap={'men-fashion':'/Images/fashion.png','women-fashion':'/Images/fashion.png','kids-fashion':'/Images/fashion.png','men-footwear':'/Images/footwear.png','women-footwear':'/Images/footwear.png','decor':'/Images/decor.png','kitchen':'/Images/home.png','audio':'/Images/tech.png','mobile-accessories':'/Images/tech.png','fitness':'/Images/sports.png','beauty':'/Images/beauty.png','books':'/Images/books.png','toys':'/Images/toys.png'}
        img=imgmap.get(cat_slug,'/Images/placeholder.png')
        short="Demo %s - quality checked."%name
        desc="Demo product '%s' in category %s. Realistic placeholder catalogue entry for the LegacyEcommerce demo store."%(name,cat_slug)
        lines.append((idx,cat_slug,name,s,sku,short,desc,price,cmp_price,stock,feat,new,img))
    return lines
def q(s): return s.replace("'","''")
def sql_for(prods, tag):
    out=["-- %s seed (idempotent: inserts only when Slug is absent)."%tag,
         "-- Generated deterministically; safe to re-run.",
         "USE LegacyEcommerceDb;","GO","-- Categories"]
    for cid,name,sl,desc,par,img,ord_ in cats:
        p='NULL' if par is None else str(par)
        out.append("IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug='%s')"%sl)
        out.append("INSERT INTO dbo.Categories(Name,Slug,Description,ImageUrl,ParentCategoryId,DisplayOrder,IsActive) VALUES (N'%s',N'%s',N'%s',N'%s',%s,%d,1);"%(q(name),sl,q(desc),img,p,ord_))
    out.append("GO")
    for (idx,cat_slug,name,s,sku,short,desc,price,cmp_price,stock,feat,new,img) in prods:
        cat_sel="(SELECT CategoryId FROM dbo.Categories WHERE Slug='%s')"%cat_slug
        cmp='NULL' if cmp_price is None else str(cmp_price)
        out.append("IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Slug='%s')"%s)
        out.append("INSERT INTO dbo.Products(Sku,Name,Slug,ShortDescription,Description,Price,CompareAtPrice,StockQuantity,CategoryId,IsFeatured,IsNewArrival,IsActive,CreatedUtc) VALUES (N'%s',N'%s',N'%s',N'%s',N'%s',%s,%s,%d,%s,%d,%d,1,SYSUTCDATETIME());"%(sku,q(name),s,q(short),q(desc),price,cmp,stock,cat_sel,feat,new))
        out.append("IF NOT EXISTS (SELECT 1 FROM dbo.ProductImages WHERE ProductId=(SELECT ProductId FROM dbo.Products WHERE Slug='%s'))"%s)
        out.append("INSERT INTO dbo.ProductImages(ProductId,ImageUrl,AltText,DisplayOrder) SELECT ProductId,N'%s',N'%s',0 FROM dbo.Products WHERE Slug='%s';"%(img,q(name),s))
    out.append("GO")
    out.append("-- Variants (sizes) for fashion and footwear demo products")
    for (idx,cat_slug,name,s,sku,short,desc,price,cmp_price,stock,feat,new,img) in prods:
        sizes=None
        if cat_slug in ('men-fashion','women-fashion','kids-fashion'): sizes=['S','M','L','XL']
        elif cat_slug in ('men-footwear','women-footwear'): sizes=['UK 6','UK 7','UK 8','UK 9']
        if sizes:
            for sz in sizes:
                out.append("IF NOT EXISTS (SELECT 1 FROM dbo.ProductVariants WHERE ProductId=(SELECT ProductId FROM dbo.Products WHERE Slug='%s') AND VariantValue='%s')"%(s,sz))
                out.append("INSERT INTO dbo.ProductVariants(ProductId,VariantName,VariantValue,Sku,PriceAdjustment,StockQuantity) SELECT ProductId,N'Size',N'%s',N'%s-%s',0,%d FROM dbo.Products WHERE Slug='%s';"%(sz,sku,sz.replace(' ','-'),max(1,stock//4),s))
    out.append("GO")
    out.append("PRINT 'Seed %s OK';"%tag)
    return "\n".join(out)
p100=gen(100); p1000=gen(1000)
open('db/seed.sql','w',encoding='utf-8').write(sql_for(p100,'demo-100'))
open('db/seed_1000.sql','w',encoding='utf-8').write(sql_for(p1000,'demo-1000'))
print(len(p100), len(p1000))
