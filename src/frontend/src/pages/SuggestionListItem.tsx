import { Fragment } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { suggestionStatusList } from '../codelist/SuggestionCodelist';
import CommentButton from '../components/CommentButton';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import LikeButton from '../components/LikeButton';
import { Suggestion } from '../interface/cms.interface';

type Props = {
    suggestion: Suggestion;
    publisher?: any;
    isLast: boolean;
    edit?: boolean;
};

const SuggestionListItem = (props: Props) => {
    const { t } = useTranslation();
    const { suggestion, isLast, publisher, edit } = props;

    return (
        <Fragment key={suggestion.id}>
            <GridRow data-testid="sr-result">
                <GridColumn widthUnits={1} totalUnits={1}>
                    <GridRow>
                        <GridColumn widthUnits={1} totalUnits={2}>
                            <Link to={'/podnet/' + suggestion.id} className="idsk-card-title govuk-link">
                                {suggestion.title}
                            </Link>
                        </GridColumn>
                        <GridColumn widthUnits={1} totalUnits={2} flexEnd>
                            {edit && (
                                <Link to={`/podnet/${suggestion.id}/upravit`} className="idsk-card-title govuk-link govuk-!-padding-right-3">
                                    {t('common.edit')}
                                </Link>
                            )}
                            <LikeButton count={suggestion.likeCount} contentId={suggestion.id} url={`cms/suggestions/likes`} />
                            <Link to={`/podnet/${suggestion.id}/komentare`} className="no-link">
                                <CommentButton count={suggestion.commentCount} />
                            </Link>
                        </GridColumn>
                    </GridRow>
                </GridColumn>
                {suggestion.description && (
                    <GridColumn widthUnits={1} totalUnits={1}>
                        <div
                            style={{
                                WebkitLineClamp: 3,
                                WebkitBoxOrient: 'vertical',
                                overflow: 'hidden',
                                textOverflow: 'ellipsis',
                                display: '-webkit-box'
                            }}
                        >
                            {suggestion.description}
                        </div>
                    </GridColumn>
                )}

                <GridColumn widthUnits={1} totalUnits={4}>
                    <span className="govuk-body-m govuk-!-font-weight-bold">
                        {suggestionStatusList?.find((status) => status.id === suggestion.status)?.label}
                    </span>
                </GridColumn>
                {publisher && (
                    <GridColumn widthUnits={3} totalUnits={4} data-testid="sr-result-publisher" flexEnd>
                        <span style={{ textAlign: 'right', color: '#777', fontStyle: 'italic', paddingLeft: '0.2rem' }}>
                            <span style={{ color: '#000', fontStyle: 'italic', fontWeight: 'bold', paddingRight: '0.2rem' }}>
                                {t('suggestionList.resolver')}:
                            </span>
                            {publisher?.label}
                        </span>
                    </GridColumn>
                )}
            </GridRow>
            {!isLast ? <hr className="idsk-search-results__card__separator" /> : null}
        </Fragment>
    );
};

export default SuggestionListItem;

SuggestionListItem.defaultProps = {
    edit: true
};
