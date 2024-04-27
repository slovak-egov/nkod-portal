import { PropsWithChildren } from 'react';
import GridColumn from './GridColumn';
import GridRow from './GridRow';
import IdSkModule from './IdSkModule';
import Loading from './Loading';

export type Props = {
    loading: boolean;
    error: Error | null;
    totalCount: number;
} & PropsWithChildren;

export default function SimpleList(props: Props) {
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
                            <GridColumn widthUnits={1} totalUnits={1}>
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
