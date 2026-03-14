export interface FeatureFlag {
  key: string
  description: string
  defaultValue: boolean
  /** When true, flag is available for localStorage override */
  overridable: boolean
}

export const FLAGS = {
  newSearch: {
    key: 'ff_newSearch',
    description: 'New Invidious search UI with filters',
    defaultValue: false,
    overridable: true,
  },
  dragReorder: {
    key: 'ff_dragReorder',
    description: 'Drag & drop queue reordering',
    defaultValue: false,
    overridable: true,
  },
  adminDashboard: {
    key: 'ff_adminDashboard',
    description: 'Admin dashboard with analytics',
    defaultValue: true,
    overridable: true,
  },
  multiDevice: {
    key: 'ff_multiDevice',
    description: 'Multi-device personal playback sessions (v2)',
    defaultValue: false,
    overridable: false,
  },
} as const satisfies Record<string, FeatureFlag>

export type FlagKey = keyof typeof FLAGS
