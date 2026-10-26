() => {
  const txt = x => String(x == null ? '' : x).replace(/\s+/g, ' ').trim();
  const selectText = sels => {
    for (const sel of sels) {
      const el = document.querySelector(sel);
      if (el && txt(el.textContent)) return txt(el.textContent);
    }
    return '';
  };
  const attr = (sel, key) => txt(document.querySelector(sel)?.getAttribute(key));

  // 1. Phân tích JSON-LD Schema.org Product
  const nodes = [];
  for (const el of document.querySelectorAll('script[type="application/ld+json"]')) {
    try {
      const payload = JSON.parse(el.textContent);
      const walk = v => {
        if (!v || typeof v !== 'object') return;
        if (Array.isArray(v)) { v.forEach(walk); return; }
        if (v['@type'] === 'Product' || (Array.isArray(v['@type']) && v['@type'].includes('Product'))) nodes.push(v);
        if (v['@graph']) walk(v['@graph']);
      };
      walk(payload);
    } catch {}
  }

  const json = nodes.find(x => x.name) || {};
  const image = json.image;
  const offer = Array.isArray(json.offers) ? json.offers[0] : (json.offers || {});
  const brandFromJson = typeof json.brand === 'object' ? json.brand?.name : json.brand;

  // 2. Bóc tách giá (Giá bán hiện tại & Giá gốc)
  const priceRe = /(?:\d{1,3}(?:[.,]\d{3})+|\d{5,10})(?:\s*(?:₫|đ|VND))?/g;
  const parseValue = val => {
    if (val === undefined || val === null || val === '') return '';
    const v = String(val).match(priceRe)?.[0];
    if (!v) return '';
    const num = v.replace(/[^0-9]/g, '');
    const n = Number(num);
    return n >= 50000 && n <= 500000000 ? String(n) : '';
  };

  const picks = Array.from(document.querySelectorAll(
    '.product__price--show, .product__price--through, .product__price--sale, [class*="product-price"], [class*="product__price"], [class*="box-product-price"], [itemprop="price"], .special-price, .base-price, .old-price'
  )).filter(el => el.getClientRects().length).map(el => txt(el.getAttribute('content') || el.textContent)).filter(Boolean);

  const prices = [...new Set(picks.flatMap(x => (x.match(priceRe) || []).map(parseValue)).filter(Boolean).map(Number))];
  const listMin = prices.length ? Math.min(...prices) : 0;
  const listMax = prices.length ? Math.max(...prices) : 0;
  const offerPrice = parseValue(offer.price || offer.lowPrice || attr('meta[property="product:price:amount"]', 'content'));
  const current = offerPrice || (listMin ? String(listMin) : '');
  const original = listMax > Number(current) ? String(listMax) : '';

  // 3. Bóc tách ảnh sản phẩm
  const imageUrl = (Array.isArray(image) ? image[0] : typeof image === 'object' ? image?.url : image) ||
    attr('meta[property="og:image"]', 'content') ||
    attr('[class*="gallery"] img', 'src') ||
    attr('[class*="box-ksp"] img', 'src') ||
    attr('main img', 'src');

  // 4. Bóc tách các phiên bản dung lượng / bộ nhớ (list-linked)
  const variantUrls = [];
  for (const a of document.querySelectorAll('.list-linked a, a.item-linked, .box-linked a')) {
    const href = a.getAttribute('href');
    if (href) {
      try {
        const u = new URL(href, location.origin);
        if (u.origin === 'https://cellphones.com.vn' && /^\/[a-z0-9-]+\.html$/i.test(u.pathname)) {
          const cleanUrl = u.origin + u.pathname;
          if (!variantUrls.includes(cleanUrl) && cleanUrl !== location.origin + location.pathname) {
            variantUrls.push(cleanUrl);
          }
        }
      } catch {}
    }
  }

  // 5. Bóc tách các phiên bản màu sắc (list-variants)
  const colors = [];
  for (const el of document.querySelectorAll('.list-variants .item-variant-name, .list-variants a[title], .box-product-variants .item-variant')) {
    const cName = txt(el.getAttribute('title') || el.textContent);
    if (cName && cName.length > 1 && cName.length < 35 && !/giá|mua|đ/i.test(cName)) {
      if (!colors.includes(cName)) colors.push(cName);
    }
  }

  // 6. Bóc tách thông số kỹ thuật (RAM, ROM, Màn hình, Chipset, Camera, Pin, Hệ điều hành...)
  const specRows = [];
  for (const tr of document.querySelectorAll('tr.technical-content-item, tr, .technical-content li, [class*="technical"] tr, [class*="specification"] tr')) {
    const tds = tr.querySelectorAll('td, th');
    let line = '';
    if (tds.length >= 2) {
      const key = txt(tds[0].textContent);
      const val = txt(tds[1].textContent);
      if (key && val && key.length < 60 && val.length < 250 && !/giá|mua ngay|khuyến mãi|xem thêm|bình luận|trả góp/i.test(key)) {
        line = `${key}: ${val}`;
      }
    } else {
      const raw = txt(tr.textContent);
      if (raw.length > 5 && raw.length < 200 && !/giá|mua ngay|khuyến mãi|xem thêm|bình luận|trả góp/i.test(raw)) {
        line = raw;
      }
    }

    if (line && !specRows.includes(line)) {
      specRows.push(line);
      if (specRows.length >= 35) break;
    }
  }

  // 7. Bóc tách mô tả tổng quan
  const baseDescription = txt(json.description || attr('meta[name="description"]', 'content') ||
    selectText(['[class*="product__highlight"]', '[class*="technical-content"]', '[class*="box-content"]']));

  let fullDescription = baseDescription;
  if (specRows.length > 0) {
    fullDescription += '\n\nThông số kỹ thuật:\n' + specRows.map(s => '• ' + s).join('\n');
  }
  if (colors.length > 0) {
    fullDescription += '\n\nMàu sắc khả dụng: ' + colors.join(', ');
  }

  // 8. Bóc tách thương hiệu (Brand)
  const brandSpec = specRows.find(t => /^Hãng sản xuất\s*[:：]?/i.test(t));
  const brandValue = brandFromJson || (brandSpec ? brandSpec.replace(/^Hãng sản xuất\s*[:：]?\s*/i, '') : '') ||
    attr('meta[property="product:brand"]', 'content');

  // 9. Tình trạng tồn kho (Availability)
  let availability = txt(offer.availability).split('/').pop() || '';
  if (!availability) {
    const body = (document.body.innerText || '').slice(0, 18000);
    if (/TẠM HẾT HÀNG|HẾT HÀNG/i.test(body)) {
      availability = 'OutOfStock';
    } else {
      availability = 'InStock';
    }
  }

  // 10. SKU
  const productId = attr('[data-product-id]', 'data-product-id');
  const skuValue = txt(json.sku || selectText(['[itemprop="sku"]', '[class*="sku"]']) || productId);

  return JSON.stringify({
    name: txt(json.name || selectText(['h1', '.product-name', '[class*="product__name"]']) || attr('meta[property="og:title"]', 'content')),
    sku: skuValue,
    brand: txt(brandValue),
    salePrice: current,
    regularPrice: original,
    description: fullDescription.slice(0, 6500),
    image: txt(imageUrl),
    availability: availability,
    variantUrls: variantUrls
  });
}
