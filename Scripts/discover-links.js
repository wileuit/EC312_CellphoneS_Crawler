() => {
  const items = new Map();
  const nonProductSlugs = new Set([
    'mobile', 'tablet', 'laptop', 'phu-kien', 'thiet-bi-am-thanh', 'dong-ho-thong-minh',
    'nha-thong-minh', 'do-gia-dung', 'tivi', 'may-tinh-de-ban', 'man-hinh', 'may-in',
    'dien-may', 'hang-cu', 'thu-cu-doi-moi', 'sim-so-dep', 'uu-dai', 'tin-tuc',
    'sforum', 'khuyen-mai', 'cart', 'order', 'hoi-dap', 'dich-vu', 'chinh-sach', 'lien-he'
  ]);

  const nonPhonePattern = /(tai-nghe|tai nghe|airpods|earbuds|headphone|buds|headset|freebuds|earphone|op-lung|ốp lưng|bao-da|bao da|cu-sac|củ sạc|cap-sac|cáp sạc|sac-nhanh|pin-du-phong|sạc dự phòng|cuong-luc|cường lực|dan-man-hinh|dán màn hình|dan-lung|gia-do|gay-chup|the-nho|thẻ nhớ|sim-|sim 4g|ipad|galaxy-tab|may-tinh-bang|máy tính bảng|macbook|laptop|apple-watch|smartwatch|dong-ho|đồng hồ|loa-bluetooth|loa bluetooth)/i;

  for (const a of document.querySelectorAll('a[href]')) {
    let u;
    try { u = new URL(a.getAttribute('href'), location.origin); } catch { continue; }
    if (u.origin !== 'https://cellphones.com.vn' || u.search || !/^\/[a-z0-9-]+\.html$/i.test(u.pathname)) continue;

    // Bỏ qua các liên kết hệ thống, bài tin tức, danh mục
    if (/\/(sforum|hoi-dap|dich-vu|chinh-sach|lien-he|khuyen-mai|uu-dai|tin-tuc|cart|order)\//i.test(u.pathname)) continue;

    const slug = u.pathname.replace(/^\/|\.html$/gi, '').toLowerCase();
    if (nonProductSlugs.has(slug)) continue;

    const card = a.closest('[class*="product-info-container"], [class*="product__box"], [class*="product-item"], [class*="product-card"], [class*="productInfo"], li');
    const name = (card?.querySelector('h3,h2,[class*="product__name"],[class*="product-name"]')?.textContent ||
      a.querySelector('h3,h2')?.textContent || a.getAttribute('title') || a.textContent || '').replace(/\s+/g, ' ').trim();
    const alt = a.querySelector('img')?.getAttribute('alt') || '';
    const candidate = name.length > 5 ? name : alt;
    const context = (candidate + ' ' + (card?.textContent || '').slice(0, 200) + ' ' + u.pathname).toLowerCase();

    // Bỏ qua nếu là phụ kiện, tai nghe, máy tính bảng...
    if (nonPhonePattern.test(context) || nonPhonePattern.test(slug)) continue;

    // Kiểm tra card sản phẩm hợp lệ
    const isInProductCard = !!card && (card.querySelector('[class*="price"], [class*="gia"]') || card.querySelector('img'));
    const isPhoneKeyword = /(dien-thoai|iphone|samsung|galaxy|xiaomi|redmi|poco|oppo|vivo|realme|honor|tecno|infinix|nokia|nothing|nubia|oneplus|asus|benco|itel|masstel|meizu|sony|tcl|huawei|smartphone)/i.test(context);

    if (!isInProductCard && !isPhoneKeyword) continue;

    const link = u.origin + u.pathname;
    if (!items.has(link)) items.set(link, { url: link, title: candidate.slice(0, 200) });
  }
  return JSON.stringify(Array.from(items.values()));
}
