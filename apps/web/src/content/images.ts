import coast480 from '../assets/images/coast-480.webp';
import coastAvif480 from '../assets/images/coast-480.avif';
import coastAvif960 from '../assets/images/coast-960.avif';
import coastAvif1122 from '../assets/images/coast-1122.avif';
import coast960 from '../assets/images/coast-960.webp';
import coast1440 from '../assets/images/coast-1440.webp';
import city480 from '../assets/images/city-480.webp';
import city960 from '../assets/images/city-960.webp';
import city1440 from '../assets/images/city-1440.webp';
import beach480 from '../assets/images/beach-480.webp';
import beach960 from '../assets/images/beach-960.webp';
import beach1440 from '../assets/images/beach-1440.webp';
import family480 from '../assets/images/family-480.webp';
import family960 from '../assets/images/family-960.webp';
import family1440 from '../assets/images/family-1440.webp';

function responsiveImage(
  small: string,
  medium: string,
  large: string,
  width: number,
  height: number,
  largeWidth = 1440,
) {
  return {
    src: medium,
    srcSet: `${small} 480w, ${medium} 960w, ${large} ${largeWidth}w`,
    width,
    height,
  };
}

export const images = {
  coast: responsiveImage(coast480, coast960, coast1440, 1122, 1402, 1122),
  city: responsiveImage(city480, city960, city1440, 1536, 1024),
  beach: responsiveImage(beach480, beach960, beach1440, 1536, 1024),
  family: responsiveImage(family480, family960, family1440, 1536, 1024),
};

export const heroAvifSrcSet = `${coastAvif480} 480w, ${coastAvif960} 960w, ${coastAvif1122} 1122w`;
