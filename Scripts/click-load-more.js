() => {
  const visible = el => !!(el.offsetWidth || el.offsetHeight || el.getClientRects().length);

  // 1. Ưu tiên nút xem thêm sản phẩm chính xác của CellphoneS
  for (const sel of ['.button__show-more-product', '.btn-show-more', '[class*="btn-showmore"] a', '[class*="show-more"]']) {
    const el = document.querySelector(sel);
    if (el && visible(el)) {
      const t = (el.innerText || '').trim();
      if (/xem thêm|sản phẩm|kết quả/i.test(t) && !/bình luận|đánh giá|bài viết|tin tức/i.test(t)) {
        el.scrollIntoView({ block: 'center', behavior: 'smooth' });
        el.click();
        return true;
      }
    }
  }

  // 2. Tìm kiếm mở rộng các thẻ chứa nội dung Xem thêm
  const all = [...document.querySelectorAll('button, a, div, span')];
  const candidates = all.filter(el => {
    if (!visible(el)) return false;
    if (el.closest('footer') || el.closest('header') || el.closest('[class*="comment"]') || el.closest('[class*="review"]')) return false;
    const t = (el.innerText || '').trim();
    return t.length > 0 && t.length < 80 && /xem thêm|xem nhiều hơn|load more|hiển thị thêm/i.test(t) && !/bình luận|đánh giá|bài viết|tin tức/i.test(t);
  });

  const preferred = candidates.find(el => /sản phẩm|kết quả/i.test(el.innerText || '')) ||
                    candidates.find(el => /button|btn|load-more|loadmore/i.test(el.className)) ||
                    candidates[0];

  if (!preferred) return false;

  preferred.scrollIntoView({ block: 'center', behavior: 'smooth' });
  preferred.click();
  return true;
}
