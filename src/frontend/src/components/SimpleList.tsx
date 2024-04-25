import { PropsWithChildren } from 'react';
import { useTranslation } from 'react-i18next';
import GridColumn from './GridColumn';
import GridRow from './GridRow';
import IdSkModule from './IdSkModule';
import Loading from './Loading';
import ResultsCount from './ResultsCount';

export type Props = {
    loading: boolean;
    error: Error | null;
    totalCount: number;
} & PropsWithChildren;

export default function SimpleList(props: Props) {
    const { t } = useTranslation();

    const totalCount = props.totalCount ?? 0;

    return (
        <IdSkModule moduleType="idsk-search-results" className="idsk-search-results">
            <GridRow>
                <GridColumn widthUnits={4} totalUnits={4} className="idsk-search-results__content">
                    {props.loading ? (
                        <Loading />
                    ) : props.error !== null ? (
                        <div>Error: {props.error.message}</div>
                    ) : (
                        <>
                            <GridColumn widthUnits={1} totalUnits={4}>
                                <span className="idsk-search-results__content__number-of-results" data-testid="sr-count">
                                    <ResultsCount count={totalCount} />
                                </span>
                            </GridColumn>

                            <GridColumn widthUnits={1} totalUnits={1}>
                                <div className="idsk-search-results__show-results__button idsk-search-results--invisible">
                                    <button className="govuk-button idsk-search-results__button-show-results" type="button">
                                        {t('show')}
                                        <ResultsCount count={props.totalCount} />
                                    </button>
                                </div>
                            </GridColumn>

                            <GridColumn widthUnits={1} totalUnits={1}>
                                <div className="idsk-search-results__content__picked-filters idsk-search-results--invisible__mobile govuk-!-margin-left-0"></div>
                                <div className="idsk-search-results__content__all">
                                    <div className="idsk-search-results__card ">
                                        <div className="idsk-card idsk-card-basic-variant nkod-search-result-card">{props.children}</div>
                                    </div>
                                </div>
                            </GridColumn>
                        </>
                    )}
                </GridColumn>
            </GridRow>
        </IdSkModule>
    );
}
