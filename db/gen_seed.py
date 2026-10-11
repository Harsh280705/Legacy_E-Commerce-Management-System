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
NAME_IMG = {
  'A Line Kurti': '/Images/products/kurti-women.jpg',
  'Action Figure': '/Images/products/action-figure-toy.jpg',
  'Ankle Boots': '/Images/products/ankle-boots-women.jpg',
  'Ankle Weights': '/Images/products/ankle-weights.jpg',
  'Apron Set': '/Images/products/kitchen-apron.jpg',
  'Art Kit': '/Images/products/art-supplies.jpg',
  'Atlas': '/Images/products/world-atlas.jpg',
  'Badminton Set': '/Images/products/badminton-racket.jpg',
  'Ballet Flats': '/Images/products/ballet-flats.jpg',
  'Banarasi Silk Saree': '/Images/products/banarasi-saree.jpg',
  'Bedside Lamp': '/Images/products/bedside-lamp.jpg',
  'Belt': '/Images/products/leather-belt.jpg',
  'Block Heel Sandals': '/Images/products/block-heel-sandals.jpg',
  'Bluetooth Headphones Pro': '/Images/products/bluetooth-headphones.jpg',
  'Bluetooth Speaker Mini': '/Images/products/bluetooth-speaker.jpg',
  'Board Game Family Pack': '/Images/products/board-game.jpg',
  'Body Lotion': '/Images/products/body-lotion-bottle.jpg',
  'Braided USB C Cable 1 5m': '/Images/products/usb-cable.jpg',
  'Brass Table Lamp': '/Images/products/table-lamp.jpg',
  'Bucket': '/Images/products/bucket.jpg',
  'Building Set': '/Images/products/building-blocks-toy.jpg',
  'Business Bestseller': '/Images/products/business-book.jpg',
  'Calculator': '/Images/products/calculator.jpg',
  'Canvas Casual Sneakers': '/Images/products/canvas-sneakers.jpg',
  'Cap': '/Images/products/cap-hat.jpg',
  'Cap Sport': '/Images/products/sports-cap.jpg',
  'Car Mount': '/Images/products/car-phone-mount.jpg',
  'Cardigan': '/Images/products/cardigan-women.jpg',
  'Cargo Pants': '/Images/products/cargo-pants.jpg',
  'Cast Iron Tawa': '/Images/products/cast-iron-pan.jpg',
  'Ceramic Vase Set': '/Images/products/ceramic-vases.jpg',
  'Chappals': '/Images/products/indian-chappal-sandals.jpg',
  'Charger Cable': '/Images/products/usb-cable.jpg',
  'Chelsea Boots': '/Images/products/chelsea-boots.jpg',
  'Chess Set': '/Images/products/chess-set.jpg',
  'Chiffon Evening Gown': '/Images/products/evening-gown.jpg',
  'Children Picture Book Set': '/Images/products/children-picture-books.jpg',
  'Chino Trousers': '/Images/products/chino-trousers.jpg',
  'Classic Cotton Kurta': '/Images/products/kurta-men.jpg',
  'Classic Indian Fiction Bestseller': '/Images/products/indian-fiction-book.jpg',
  'Cloth Hanger': '/Images/products/clothes-hangers.jpg',
  'Comic Collection': '/Images/products/comic-books.jpg',
  'Compact Powder': '/Images/products/compact-powder-makeup.jpg',
  'Competitive Exam Guide': '/Images/products/exam-guide-book.jpg',
  'Cookbook Regional Classics': '/Images/products/cookbook.jpg',
  'Cotton Anarkali Kurti': '/Images/products/anarkali-kurti.jpg',
  'Cotton Bedsheet King Size': '/Images/products/bedsheet-bed.jpg',
  'Cotton Casual T Shirt': '/Images/products/t-shirt.jpg',
  'Court Shoes': '/Images/products/court-shoes-sneakers.jpg',
  'Cricket Bat': '/Images/products/cricket-bat.jpg',
  'Cricket Kit': '/Images/products/cricket-kit.jpg',
  'Curtain Pair': '/Images/products/curtains-window.jpg',
  'Cushion Covers': '/Images/products/cushions-sofa.jpg',
  'Cushion Covers Set of': '/Images/products/cushions-sofa.jpg',
  'Denim Jacket': '/Images/products/denim-jacket.jpg',
  'Denim Shirt': '/Images/products/denim-shirt.jpg',
  'Deodorizer': '/Images/products/sneakers.jpg',
  'Derby Shoes': '/Images/products/derby-shoes.jpg',
  'Dictionary': '/Images/products/dictionary-book.jpg',
  'Dinner Mats': '/Images/products/table-placemats.jpg',
  'Doll House Playset': '/Images/products/dollhouse.jpg',
  'Doll Set': '/Images/products/dolls-toys.jpg',
  'Doormat': '/Images/products/doormat.jpg',
  'Dumbbell Set 10kg': '/Images/products/dumbbells.jpg',
  'Dumbbells Pair': '/Images/products/dumbbells.jpg',
  'Dustbin': '/Images/products/trash-bin.jpg',
  'Earbuds Case': '/Images/products/earbuds-case.jpg',
  'Earbuds Lite': '/Images/products/wireless-earbuds.jpg',
  'Earphones Sport': '/Images/products/earphones.jpg',
  'Electric Kettle 1 8L': '/Images/products/electric-kettle.jpg',
  'Embroidered Juttis': '/Images/products/embroidered-juttis.jpg',
  'Embroidered Lehenga Choli': '/Images/products/lehenga.jpg',
  'Exercise Ball': '/Images/products/exercise-ball.jpg',
  'Extension Board': '/Images/products/power-strip.jpg',
  'Face Mask Pack': '/Images/products/face-mask-skincare.jpg',
  'Face Serum': '/Images/products/face-serum-bottle.jpg',
  'Fast Charger 25W': '/Images/products/phone-charger.jpg',
  'Festive Silk Kurta': '/Images/products/silk-kurta.jpg',
  'Flip Flops': '/Images/products/flip-flops.jpg',
  'Floaters': '/Images/products/floaters-sandals.jpg',
  'Floral Summer Dress': '/Images/products/floral-dress.jpg',
  'Foam Roller': '/Images/products/foam-roller.jpg',
  'Football': '/Images/products/football-soccer-ball.jpg',
  'Formal Trousers': '/Images/products/formal-trousers-men.jpg',
  'Frock': '/Images/products/kids-frock-dress.jpg',
  'Frying Pan': '/Images/products/frying-pan.jpg',
  'Glass Storage Jars Set of': '/Images/products/glass-storage-jars.jpg',
  'Graphic Tee': '/Images/products/kids-t-shirts.jpg',
  'Gym Bag': '/Images/products/gym-bag.jpg',
  'Gym Shaker 700ml': '/Images/products/gym-shaker-bottle.jpg',
  'Hair Conditioner': '/Images/products/shampoo-conditioner-bottles.jpg',
  'Hair Oil with Amla 200ml': '/Images/products/hair-oil-bottle.jpg',
  'Handkerchief Pack': '/Images/products/handkerchief.jpg',
  'Handloom Cotton Saree': '/Images/products/cotton-saree.jpg',
  'Handwoven Wall Hanging': '/Images/products/wall-hanging-decor.jpg',
  'Headphones Studio': '/Images/products/headphones.jpg',
  'Heel Grips': '/Images/products/high-heels.jpg',
  'Herbal Face Wash 150ml': '/Images/products/face-wash.jpg',
  'High Top Sneakers': '/Images/products/high-top-sneakers.jpg',
  'History of India': '/Images/products/india-history-book.jpg',
  'Indigo Denim Jeans': '/Images/products/denim-jeans.jpg',
  'Insoles Pair': '/Images/products/shoe-insoles.jpg',
  'Insulated Flask 1L': '/Images/products/thermos-flask.jpg',
  'Iron Box': '/Images/products/clothes-iron.jpg',
  'Jacket': '/Images/products/jacket.jpg',
  'Jeans': '/Images/products/jeans.jpg',
  'Jump Rope': '/Images/products/jump-rope.jpg',
  'Kajal Eyeliner Combo': '/Images/products/eyeliner-kajal.jpg',
  'Kettle Mini': '/Images/products/electric-kettle.jpg',
  'Kettlebell': '/Images/products/kettlebell.jpg',
  'Kids Cotton T Shirt Pack': '/Images/products/kids-t-shirts.jpg',
  'Kids Denim Dungaree': '/Images/products/kids-dungaree.jpg',
  'Kids Festive Kurta': '/Images/products/kids-kurta.jpg',
  'Kids Hooded Sweatshirt': '/Images/products/kids-hoodie.jpg',
  'Kids Party Frock': '/Images/products/kids-party-dress.jpg',
  'Kids School Shorts Set': '/Images/products/kids-shorts.jpg',
  'Kitten Heels': '/Images/products/kitten-heels.jpg',
  'Knife Set': '/Images/products/kitchen-knives-set.jpg',
  'Knitted Sneakers': '/Images/products/knit-sneakers.jpg',
  'Kolhapuris': '/Images/products/kolhapuri-sandals.jpg',
  'Kurta Pyjama Set': '/Images/products/kurta-pyjama-men.jpg',
  'LED Bulb Pack': '/Images/products/led-bulb.jpg',
  'Laces Pack': '/Images/products/shoelaces-sneakers.jpg',
  'Laundry Basket': '/Images/products/laundry-basket.jpg',
  'Leather Formal Oxfords': '/Images/products/oxford-shoes-leather.jpg',
  'Leggings Pair': '/Images/products/leggings.jpg',
  'Linen Summer Shirt': '/Images/products/linen-shirt.jpg',
  'Lip Balm Duo': '/Images/products/lip-balm.jpg',
  'Loafers': '/Images/products/loafers-shoes.jpg',
  'Lunch Box': '/Images/products/lunch-box.jpg',
  'Macrame Plant Hangers': '/Images/products/macrame-plant-hanger.jpg',
  'Mary Janes': '/Images/products/mary-jane-shoes.jpg',
  'Masala Dabba 7 Bowls': '/Images/products/masala-dabba.jpg',
  'Matte Lipstick Set': '/Images/products/lipstick.jpg',
  'Maxi Dress': '/Images/products/maxi-dress.jpg',
  'Microphone': '/Images/products/microphone.jpg',
  'Mirror Frame': '/Images/products/mirror-frame.jpg',
  'Mixer Grinder 750W': '/Images/products/mixer-grinder.jpg',
  'Mobile Tripod Stand': '/Images/products/phone-tripod.jpg',
  'Moisturising Cream 200ml': '/Images/products/moisturizer-cream.jpg',
  'Mop Set': '/Images/products/mop-cleaning.jpg',
  'Mystery Novel': '/Images/products/mystery-novel-book.jpg',
  'Nail Polish Set': '/Images/products/nail-polish-bottles.jpg',
  'Neckband Earphones': '/Images/products/neckband-earphones.jpg',
  'Nehru Jacket': '/Images/products/nehru-jacket.jpg',
  'Night Suit': '/Images/products/pajamas.jpg',
  'Non Stick Cookware Set 5pc': '/Images/products/nonstick-cookware.jpg',
  'Notebook Set Pack of': '/Images/products/notebooks.jpg',
  'OTG Adapter': '/Images/products/usb-adapter-cable.jpg',
  'Oxford Shirt': '/Images/products/men-oxford-shirt.jpg',
  'Palazzo Set': '/Images/products/palazzo-pants.jpg',
  'Phone Back Cover': '/Images/products/phone-case.jpg',
  'Phone Stand': '/Images/products/phone-stand.jpg',
  'Photo Frame Set': '/Images/products/photo-frames.jpg',
  'Plush Elephant Soft Toy': '/Images/products/plush-elephant.jpg',
  'Poetry Collection': '/Images/products/poetry-book.jpg',
  'Polo T Shirt': '/Images/products/polo-t-shirt.jpg',
  'Portable Bluetooth Speaker': '/Images/products/portable-speaker.jpg',
  'Power Bank 20000mAh': '/Images/products/power-bank.jpg',
  'Pressure Cooker': '/Images/products/pressure-cooker.jpg',
  'Pull Up Bar': '/Images/products/pull-up-bar.jpg',
  'Puzzle 500 Pieces': '/Images/products/jigsaw-puzzle.jpg',
  'Radio Retro': '/Images/products/retro-radio.jpg',
  'Remote Control Car': '/Images/products/remote-control-car.jpg',
  'Resistance Bands Set of': '/Images/products/resistance-bands.jpg',
  'Robot Toy': '/Images/products/robot-toy.jpg',
  'Running Shoes': '/Images/products/running-shoes.jpg',
  'Running Sports Shoes': '/Images/products/running-shoes.jpg',
  'Scarf Set': '/Images/products/scarf.jpg',
  'Scented Soy Candles Set of': '/Images/products/scented-candles.jpg',
  'Science Encyclopedia': '/Images/products/encyclopedia-books.jpg',
  'Screen Guard': '/Images/products/phone-screen-protector.jpg',
  'Self Help Paperback': '/Images/products/self-help-book.jpg',
  'Selfie Stick': '/Images/products/selfie-stick.jpg',
  'Serving Tray': '/Images/products/serving-tray.jpg',
  'Shirt': '/Images/products/shirt.jpg',
  'Shoe Bag': '/Images/products/shoe-bag-travel.jpg',
  'Shoe Horn': '/Images/products/dress-shoes-leather.jpg',
  'Shoe Polish Kit': '/Images/products/shoe-polish.jpg',
  'Shorts': '/Images/products/shorts.jpg',
  'Silk Blouse': '/Images/products/silk-blouse.jpg',
  'Skater Skirt': '/Images/products/skater-skirt.jpg',
  'Skipping Rope': '/Images/products/skipping-rope.jpg',
  'Skipping Rope Pro': '/Images/products/skipping-rope.jpg',
  'Skirt Set': '/Images/products/kids-skirt.jpg',
  'Slim Fit Formal Shirt': '/Images/products/formal-shirt-men.jpg',
  'Slip On Sneakers': '/Images/products/slip-on-sneakers.jpg',
  'Slip Ons': '/Images/products/slip-on-shoes.jpg',
  'Socks Pack': '/Images/products/socks.jpg',
  'Soundbar': '/Images/products/soundbar.jpg',
  'Soundbar 60W': '/Images/products/soundbar.jpg',
  'Spice Box': '/Images/products/spice-box.jpg',
  'Sports Sandals': '/Images/products/sports-sandals.jpg',
  'Sports Shoes': '/Images/products/sports-shoes.jpg',
  'Stainless Steel Dinner Set 24pc': '/Images/products/dinner-set-plates.jpg',
  'Storage Box Set': '/Images/products/storage-boxes.jpg',
  'Straight Fit Jeans': '/Images/products/women-jeans.jpg',
  'Straight Pants': '/Images/products/women-trousers.jpg',
  'Suede Loafers': '/Images/products/suede-loafers.jpg',
  'Sunglasses': '/Images/products/sunglasses.jpg',
  'Sunscreen SPF50': '/Images/products/sunscreen-bottle.jpg',
  'Sweater': '/Images/products/sweater.jpg',
  'Sweatshirt': '/Images/products/sweatshirt.jpg',
  'Table Fan': '/Images/products/table-fan.jpg',
  'Table Runner': '/Images/products/table-runner.jpg',
  'Teddy Bear': '/Images/products/teddy-bear.jpg',
  'Tempered Glass Pack of': '/Images/products/smartphone.jpg',
  'Tennis Balls': '/Images/products/tennis-balls.jpg',
  'Tie Set': '/Images/products/necktie.jpg',
  'Toner': '/Images/products/facial-toner-bottle.jpg',
  'Torch': '/Images/products/flashlight.jpg',
  'Towel Set': '/Images/products/bath-towels.jpg',
  'Track Pants': '/Images/products/track-pants.jpg',
  'Track Suit': '/Images/products/tracksuit.jpg',
  'Traditional Mojari': '/Images/products/mojari-juttis.jpg',
  'Trekking Boots': '/Images/products/trekking-boots.jpg',
  'Trimmer': '/Images/products/hair-trimmer.jpg',
  'True Wireless Earbuds': '/Images/products/wireless-earbuds.jpg',
  'Tunic Top': '/Images/products/tunic-top-women.jpg',
  'Turntable': '/Images/products/turntable-vinyl.jpg',
  'Vase': '/Images/products/vase.jpg',
  'Waistcoat': '/Images/products/waistcoat-men.jpg',
  'Wall Clock': '/Images/products/wall-clock.jpg',
  'Wallet': '/Images/products/leather-wallet.jpg',
  'Water Bottle': '/Images/products/water-bottle.jpg',
  'Water Bottle Sport': '/Images/products/water-bottle.jpg',
  'Waterproof Spray': '/Images/products/sneakers-shoes.jpg',
  'Wedge Heels': '/Images/products/wedge-heels.jpg',
  'Wired Earphones with Mic': '/Images/products/wired-earphones.jpg',
  'Wireless Charger': '/Images/products/wireless-charger.jpg',
  'Wooden Building Blocks 100pc': '/Images/products/wooden-building-blocks.jpg',
  'Wooden Photo Frames Set of': '/Images/products/wooden-photo-frames.jpg',
  'Wool Blend Blazer': '/Images/products/blazer-men.jpg',
  'Wrap Top': '/Images/products/women-top.jpg',
  'Wrist Band': '/Images/products/wristband.jpg',
  'Yoga Mat 6mm Anti Skid': '/Images/products/yoga-mat.jpg',
}

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
        img=NAME_IMG.get(_imgkey(name),'/Images/placeholder.png')
        short="Demo %s - quality checked."%name
        desc="Demo product '%s' in category %s. Realistic placeholder catalogue entry for the LegacyEcommerce demo store."%(name,cat_slug)
        lines.append((idx,cat_slug,name,s,sku,short,desc,price,cmp_price,stock,feat,new,img))
    return lines
def _imgkey(n):
    n=re.sub(r' Mk\d+$','',n); n=re.sub(r'[^A-Za-z0-9 ]',' ',n); n=re.sub(r'\s+',' ',n).strip()
    t=n.split()
    while t and t[-1].isdigit(): t=t[:-1]
    return ' '.join(t)
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
