import { t } from 'i18next';
import GridColumn from '../components/GridColumn';

type Props = {
    value: React.ReactNode;
    labelKey?: string;
};

export default function DetailItemElement(props: Props) {
    const { value, labelKey } = props;
    return (
        <GridColumn widthUnits={1} totalUnits={1}>
            <div className="nkod-detail-attribute">
                {labelKey && <div className="govuk-body nkod-detail-attribute-name">{t(labelKey)}</div>}
                <div className="govuk-body nkod-detail-attribute-value">{value}</div>
            </div>
        </GridColumn>
    );
}
