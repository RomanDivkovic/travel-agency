import {
  ArrowRight,
  ArrowUpRight,
  BedDouble,
  Clock3,
  Compass,
  Heart,
  Luggage,
  Menu,
  Plane,
  UserRound,
  Wallet,
  X,
} from 'lucide-react';

const icons = {
  arrow: ArrowRight,
  arrowUp: ArrowUpRight,
  bed: BedDouble,
  clock: Clock3,
  compass: Compass,
  heart: Heart,
  luggage: Luggage,
  menu: Menu,
  plane: Plane,
  user: UserRound,
  wallet: Wallet,
  close: X,
};
export type IconName = keyof typeof icons;

export function Icon({
  name,
  size = 24,
  className,
}: {
  name: IconName;
  size?: number;
  className?: string;
}) {
  const Symbol = icons[name];
  return (
    <Symbol
      size={size}
      strokeWidth={1.5}
      className={className}
      aria-hidden="true"
      focusable="false"
    />
  );
}
