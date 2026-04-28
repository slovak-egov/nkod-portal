import GridColumn from '../../components/GridColumn';
import GridRow from '../../components/GridRow';
import PageSubheader from '../../components/PageSubHeader';
import { formatNumber } from './tools';
import { IntColumn } from './types';

type Props = {
    datasetsCount?: IntColumn | null | undefined;
    newDatasetsCount?: IntColumn | null | undefined;
    modifiedDatasetsCount?: IntColumn | null | undefined;
    removedDatasetsCount?: IntColumn | null | undefined;
};

export default function DatasetsCountPanel(props: Props) {
    const { datasetsCount, newDatasetsCount, modifiedDatasetsCount, removedDatasetsCount } = props;

    return (
        <>
            <PageSubheader>Datasety</PageSubheader>
            <GridRow>
                <GridColumn widthUnits={1} totalUnits={4}>
                    <div className="nkod-detail-attribute">
                        <div className="govuk-body nkod-detail-attribute-name">Celkový počet</div>
                        <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                            {formatNumber(datasetsCount?.value)}
                        </div>
                    </div>
                </GridColumn>
                <GridColumn widthUnits={1} totalUnits={4}>
                    <div className="nkod-detail-attribute">
                        <div className="govuk-body nkod-detail-attribute-name">Nové</div>
                        <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                            {formatNumber(newDatasetsCount?.value)}
                        </div>
                    </div>
                </GridColumn>
                <GridColumn widthUnits={1} totalUnits={4}>
                    <div className="nkod-detail-attribute">
                        <div className="govuk-body nkod-detail-attribute-name">Upravené</div>
                        <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                            {formatNumber(modifiedDatasetsCount?.value)}
                        </div>
                    </div>
                </GridColumn>
                <GridColumn widthUnits={1} totalUnits={4}>
                    <div className="nkod-detail-attribute">
                        <div className="govuk-body nkod-detail-attribute-name">Odstránené</div>
                        <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                            {formatNumber(removedDatasetsCount?.value)}
                        </div>
                    </div>
                </GridColumn>
            </GridRow>
        </>
    );
}
