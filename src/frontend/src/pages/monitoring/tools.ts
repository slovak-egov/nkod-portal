import moment from 'moment';

export type SparqlQuerySorting = { propertyName: string; ascending: boolean };

export type SparqlQuery = {
    prefixes?: string[] | null | undefined;
    select: string[];
    where?: string[] | null | undefined;
    defaultOrderBy?: SparqlQuerySorting[] | null | undefined;
};

export type SparqlResult<T> = {
    results: {
        bindings: T[];
    };
};
export type ExportDefintion<T> = {
    columns: { header: string; value: (row: T) => string | number }[];
};

export function convertXsdDurationToSeconds(duration: string | null | undefined): number | null {
    if (!duration) return null;
    const regex = /^(-?)P(?:(\d+)Y)?(?:(\d+)M)?(?:(\d+)D)?(?:T(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?)?$/;

    const match = duration.match(regex);
    if (!match) return null;

    const d = {
        negative: match[1] === '-',
        years: parseInt(match[2] ?? 0),
        months: parseInt(match[3] ?? 0),
        days: parseInt(match[4] ?? 0),
        hours: parseInt(match[5] ?? 0),
        minutes: parseInt(match[6] ?? 0),
        seconds: parseFloat(match[7] ?? 0)
    };

    let totalSeconds = d.seconds;
    totalSeconds += d.minutes * 60;
    totalSeconds += d.hours * 3600;
    totalSeconds += d.days * 86400;
    totalSeconds += d.months * 30 * 86400;
    totalSeconds += d.years * 365 * 86400;
    if (d.negative) totalSeconds = -totalSeconds;
    return totalSeconds;
}

export function formatSecondsToMs(seconds: number | null): string {
    if (seconds === null) return '';
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = Math.floor(seconds % 60);
    return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
}

export function formatSecondsToHms(seconds: number | null): string {
    if (seconds === null) return '';
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const remainingSeconds = Math.floor(seconds % 60);
    return `${hours}:${minutes.toString().padStart(2, '0')}:${remainingSeconds.toString().padStart(2, '0')}`;
}

export function formatSecondsToMilisecondsOrSeconds(seconds: number | null): string {
    if (seconds === null) return '';
    if (seconds < 60) {
        const milliseconds = Math.round(seconds * 1000);
        return `${milliseconds} ms`;
    } else {
        return `${Math.round(seconds)} s`;
    }
}

export async function runSparqlQuery<T>(
    endpointUrl: string | null | undefined,
    query: SparqlQuery,
    limit?: number | null | undefined,
    offset?: number | null | undefined,
    sorting?: SparqlQuerySorting[] | null | undefined
): Promise<T[]> {
    if (!endpointUrl) return [];

    let q = (query.prefixes ?? []).map((p) => `PREFIX ${p}`).join('\n');

    q += `\nSELECT ${query.select.join(' ')}\n`;

    if (query.where) {
        q += `WHERE {\n${query.where.map((w) => `${w}`).join('\n')}\n}\n`;
    }

    const effectiveSorting = sorting && sorting.length > 0 ? sorting : (query.defaultOrderBy ?? []);
    if (effectiveSorting.length > 0) {
        q += 'ORDER BY ' + effectiveSorting.map((s) => (s.ascending ? s.propertyName : `DESC(${s.propertyName})`)).join(' ') + '\n';
    }

    if (limit !== null && limit !== undefined && limit > 0) {
        q += `LIMIT ${limit}\n`;
    }

    if (offset !== null && offset !== undefined) {
        q += `OFFSET ${offset}\n`;
    }

    const response = await fetch(endpointUrl + '?query=' + encodeURIComponent(q), {
        headers: { Accept: 'application/sparql-results+json' }
    });
    const result: SparqlResult<T> = await response.json();
    return result.results.bindings;
}

export function formatDateTime(dateTime: string | null | undefined): string {
    if (!dateTime) return '';
    return moment(dateTime).local().format('DD. MM. YYYY HH:mm:ss');
}

export function formatDate(dateTime: string | null | undefined): string {
    if (!dateTime) return '';
    return moment(dateTime).local().format('DD. MM. YYYY');
}

export function getNumber(value: number | string | null | undefined): number | null {
    if (value === null || value === undefined) return null;
    return typeof value === 'number' ? value : parseFloat(value);
}

export function formatNumber(value: number | string | null | undefined): string {
    const n = getNumber(value);
    if (n === null) return '';
    return n.toLocaleString('sk', { useGrouping: true });
}
