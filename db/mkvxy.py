import io
D = r'D:\Yantra3loka LLP\Training\Week 3 (5 October to 11 October)\Legacy E-Commerce'
base = open(D + r'\..\LegacyEcommerce-backup-20261011\Ecommerce.Web\Views\Shared\_Header.cshtml', encoding='utf-8').read()
old_li = '<li id="mini-cart-slot"><a href="/Cart/Index" class="yb-cart-link">Cart (<span id="cart-count">0</span>)</a></li>'
assert old_li in base
# V-X: only line 44 gains id + aria attributes (no panel div)
vx = base.replace(old_li, '<li id="mini-cart-slot"><a href="/Cart/Index" id="mini-cart-btn" class="yb-cart-link" aria-haspopup="true" aria-expanded="false" aria-controls="mini-cart-panel">Cart (<span id="cart-count">0</span>)</a></li>')
open(r'C:\Users\Harsh\AppData\Local\Temp\opencode\hdr_vx.cshtml', 'w', encoding='utf-8').write(vx)
# V-Y: only panel div added after </ul> block (line 44 untouched)
anchor = '                </ul>\n            </div>\n        </div>\n    </nav>'
panel = ('                </ul>\n            </div>\n'
         '            <div id="mini-cart-panel" class="yb-minicart-panel" style="display:none">\n'
         '                <div id="mini-cart-body"><p class="yb-empty">Loading cart...</p></div>\n'
         '            </div>\n        </div>\n    </nav>')
assert anchor in base
vy = base.replace(anchor, panel)
open(r'C:\Users\Harsh\AppData\Local\Temp\opencode\hdr_vy.cshtml', 'w', encoding='utf-8').write(vy)
print('vx, vy ok')
