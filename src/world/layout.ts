/** Shared street grid layout. Units are meters, human scale. */
export const MAIN_ROAD_WIDTH = 9;
export const SIDEWALK_WIDTH = 4;
export const ROAD_LENGTH = 200;

export const CROSS_ROAD_CENTER_X = 8;
export const CROSS_ROAD_WIDTH = 8;

/** Building fronts sit on these lines. */
export const NORTH_FRONT_Z = -(MAIN_ROAD_WIDTH / 2 + SIDEWALK_WIDTH);
export const SOUTH_FRONT_Z = MAIN_ROAD_WIDTH / 2 + SIDEWALK_WIDTH;

export const ROAD_TOP_Y = 0.14;
export const SIDEWALK_TOP_Y = 0.34;
