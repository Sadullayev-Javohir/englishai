/** Text part of the approved EnglishAI lockup; hidden on mobile. */
export function BrandName({ className }: { className?: string }) {
  return <span className={className ? `ea-brand-name ${className}` : "ea-brand-name"}>EnglishAI</span>;
}
