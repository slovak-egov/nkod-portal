import GridColumn from '../../components/GridColumn';
import GridRow from '../../components/GridRow';
import PageSubheader from '../../components/PageSubHeader';
import {
    convertXsdDurationToSeconds,
    formatNumber,
    formatSecondsToMilisecondsOrSeconds,
    formatSecondsToMs,
    getNumber,
} from './tools';
import { DecimalColumn, DurationColumn, IntColumn } from './types';

type Props = {
    count?: IntColumn | null | undefined;
    totalDuration?: DurationColumn | null | undefined;
    averageDuration?: DurationColumn | null | undefined;
    successRate?: DecimalColumn | null | undefined;
    title: string;
};

export default function ExternalSourcesPanel(props: Props) {
    const { count, totalDuration, averageDuration, successRate } = props;

    const ratio = getNumber(successRate?.value);
    const successRateText = ratio !== null ? (ratio * 100).toLocaleString('sk', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + '%' : '-';

    return (
        <>
            <PageSubheader>
                {props.title} {count?.value ? `(${formatNumber(count.value)})` : ''}
            </PageSubheader>
            <GridRow>
                <GridColumn widthUnits={1} totalUnits={4}>
                    <div className="nkod-detail-attribute">
                        <div className="govuk-body nkod-detail-attribute-name">Celková doba získania</div>
                        <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                            {formatSecondsToMs(convertXsdDurationToSeconds(totalDuration?.value))}
                        </div>
                    </div>
                </GridColumn>
                <GridColumn widthUnits={1} totalUnits={4}>
                    <div className="nkod-detail-attribute">
                        <div className="govuk-body nkod-detail-attribute-name">Priemerná doba odozvy</div>
                        <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                            {formatSecondsToMilisecondsOrSeconds(convertXsdDurationToSeconds(averageDuration?.value))}
                        </div>
                    </div>
                </GridColumn>
                <GridColumn widthUnits={1} totalUnits={4}>
                    <div className="nkod-detail-attribute">
                        <div className="govuk-body nkod-detail-attribute-name">Úspešnosť</div>
                        <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                            {successRateText}
                        </div>
                    </div>
                </GridColumn>
            </GridRow>
        </>
    );
}
