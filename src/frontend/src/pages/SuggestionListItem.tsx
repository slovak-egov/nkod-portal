import { Fragment } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useUserPermissions } from '../client';
import CommentButton from '../components/CommentButton';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import LikeButton from '../components/LikeButton';
import { Suggestion, SuggestionStatusCode } from '../interface/cms.interface';

type Props = {
    suggestion: Suggestion;
    publisher?: any;
    isLast: boolean;
    editable?: boolean;
};

const SuggestionListItem = (props: Props) => {
    const { t } = useTranslation();
    const { isCommunityUser, isSuperAdmin, isMine, isPublisher, isMineOrg } = useUserPermissions();
    const { suggestion, isLast, publisher, editable } = props;

    const showEdit =
        editable &&
        (isSuperAdmin ||
            ((isCommunityUser || isPublisher) && isMine(suggestion.userId) && suggestion.status === SuggestionStatusCode.CREATED) ||
            (isPublisher && isMineOrg(suggestion.orgToUri)));

    return (
        <Fragment key={suggestion.id}>
            <GridRow data-testid="sr-result">
                <GridColumn widthUnits={1} totalUnits={1}>
                    <GridRow>
                        <GridColumn widthUnits={3} totalUnits={4}>
                            <Link to={'/podnet/' + suggestion.id} className="idsk-card-title govuk-link">
                                {suggestion.title}
                            </Link>
                        </GridColumn>
                        <GridColumn widthUnits={1} totalUnits={4} flexEnd>
                            {showEdit && (
                                <Link to={`/podnet/${suggestion.id}/upravit`} className="idsk-card-title govuk-link">
                                    {t('common.edit')}
                                </Link>
                            )}
                            <LikeButton count={suggestion.likeCount} contentId={suggestion.id} url={`suggestions/likes`} />
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
                    <span className="govuk-body-m govuk-!-font-weight-bold">{t(`codelists.suggestionStatus.${suggestion.status}`) ?? ''}</span>
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
    editable: true
};
